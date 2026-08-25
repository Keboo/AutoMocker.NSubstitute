using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using NSubstitute.AutoMock.Extensions;
using NSubstitute.AutoMock.Resolvers;
using NSubstitute.Core;

namespace NSubstitute.AutoMock;

/// <summary>
/// An auto-mocking IoC container that generates substitute objects using NSubstitute.
/// </summary>
public partial class AutoMocker : IServiceProvider
{
    /// <summary>
    /// Initializes an instance of AutoMocker.
    /// </summary>
    public AutoMocker()
        : this(callBase: false)
    {
    }

    /// <summary>
    /// Initializes an instance of AutoMocker.
    /// </summary>
    /// <param name="callBase">Whether class substitutes are created as partial substitutes
    /// (equivalent to <c>Substitute.ForPartsOf</c>) so that base implementations are invoked
    /// for members without configured return values.</param>
    public AutoMocker(bool callBase)
    {
        CallBase = callBase;

        Resolvers =
        [
            new CacheResolver(),
            new CallbackResolver(),
            new SelfResolver(),
            new ArrayResolver(),
            new AutoMockerDisposableResolver(),
            new EnumerableResolver(),
            new LazyResolver(),
            new FuncResolver(),
            new CancellationTokenResolver(),
            new HttpMessageHandlerWrapperResolver(),
            new HttpClientResolver(),
            new SubstituteResolver(callBase)
        ];
    }

    /// <summary>
    /// Whether class substitutes are created as partial substitutes
    /// (equivalent to <c>Substitute.ForPartsOf</c>) so that base implementations are invoked
    /// for members without configured return values. Defaults to <c>false</c>.
    /// </summary>
    public bool CallBase { get; }

    /// <summary>
    /// A collection of resolves determining how a given dependency will be resolved.
    /// </summary>
    public IList<ISubstituteResolver> Resolvers { get; }

    /// <summary>
    /// A collection of objects stored in this AutoMocker instance.
    /// The keys are the types used when resolving services.
    /// </summary>
    public IReadOnlyDictionary<Type, object?> ResolvedObjects
        //NB: NonBlocking.ConcurrentDictionary GetEnumerator method returns a snapshot enumerator which is thread-safe
        => TypeMap?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value) ?? [];

    private NonBlocking.ConcurrentDictionary<Type, IInstance>? TypeMap
        => Resolvers.OfType<CacheResolver>().FirstOrDefault()?.TypeMap;

    private bool TryResolve(Type serviceType,
        ObjectGraphContext resolutionContext,
        [NotNullWhen(true)] out IInstance? instance,
        out bool noCache)
    {
        if (resolutionContext.VisitedTypes.Contains(serviceType))
        {
            //Rejected due to circular dependency
            instance = null;
            noCache = false;
            return false;
        }

        resolutionContext.VisitedTypes.Add(serviceType);
        var context = new SubstituteResolutionContext(this, serviceType, resolutionContext);

        List<ISubstituteResolver> resolvers = [.. Resolvers];
        for (int i = 0; i < resolvers.Count && !context.ValueProvided; i++)
        {
            try
            {
                resolvers[i].Resolve(context);
            }
            catch (Exception ex)
            {
                resolutionContext.AddDiagnosticMessage($"Resolver: {resolvers[i].GetType().FullName} threw an exception while attempting to resolve service of type {serviceType.AssemblyQualifiedName} {ex}");
            }
        }

        if (!context.ValueProvided)
        {
            instance = null;
            noCache = false;
            return false;
        }

        instance = context.Value switch
        {
            IInstance i => i,
            { } value when IsSubstitute(value) => new SubstituteInstance(value),
            _ => new RealInstance(context.Value),
        };
        noCache = context.NoCache;
        return true;
    }

    #region Create Instance

    /// <summary>
    /// Constructs an instance from known services. Any dependencies (constructor arguments)
    /// are fulfilled by searching the container or, if not found, automatically generating
    /// substitutes.
    /// </summary>
    /// <typeparam name="T">A concrete type</typeparam>
    /// <returns>An instance of T with all constructor arguments derived from services 
    /// setup in the container.</returns>
    public T CreateInstance<T>() where T : class
        => CreateInstance<T>(false);

    /// <summary>
    /// Constructs an instance from known services. Any dependencies (constructor arguments)
    /// are fulfilled by searching the container or, if not found, automatically generating
    /// substitutes.
    /// </summary>
    /// <typeparam name="T">A concrete type</typeparam>
    /// <param name="enablePrivate">When true, non-public constructors will also be used to create instances.</param>
    /// <returns>An instance of T with all constructor arguments derived from services 
    /// setup in the container.</returns>
    public T CreateInstance<T>(bool enablePrivate) where T : class
        => (T)CreateInstance(typeof(T), enablePrivate);

    /// <summary>
    /// Constructs an instance from known services. Any dependencies (constructor arguments)
    /// are fulfilled by searching the container or, if not found, automatically generating
    /// substitutes.
    /// </summary>
    /// <param name="type">A concrete type</param>
    /// <returns>An instance of type with all constructor arguments derived from services 
    /// setup in the container.</returns>
    public object CreateInstance(Type type) => CreateInstance(type, false);

    /// <summary>
    /// Constructs an instance from known services. Any dependencies (constructor arguments)
    /// are fulfilled by searching the container or, if not found, automatically generating
    /// substitutes.
    /// </summary>
    /// <param name="type">A concrete type</param>
    /// <param name="enablePrivate">When true, non-public constructors will also be used to create instances.</param>
    /// <returns>An instance of type with all constructor arguments derived from services 
    /// setup in the container.</returns>
    public object CreateInstance(Type type, bool enablePrivate)
    {
        if (type is null) throw new ArgumentNullException(nameof(type));

        var context = new ObjectGraphContext(enablePrivate);

        return CreateInstanceInternal(type, context);
    }

    internal object CreateInstanceInternal(Type type, ObjectGraphContext context)
    {
        if (!TryGetConstructorInvocation(type, context, out ConstructorInfo? ctor, out IInstance[]? arguments))
        {
            throw new ObjectCreationException(
                $"Did not find a best constructor for `{type}`. If any type in the hierarchy has a non-public constructor, set the 'enablePrivate' parameter to true for this {nameof(AutoMocker)} method.",
                context.DiagnosticMessages);
        }

        try
        {
            object?[] parameters = [.. arguments.Select(x => x.Value)];
            return ctor.Invoke(parameters);
        }
        catch (TargetInvocationException e)
        {
            ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw();
            throw;  //Not really reachable either way, but I like this better than return default(T) 
        }
    }

    #endregion Create Instance

    #region CreateSelfSubstitute

    /// <summary> 
    /// Constructs a self-substitute from the services available in the container. A self-substitute is 
    /// a partial substitute (<c>Substitute.ForPartsOf</c>) whose non-configured members invoke the real 
    /// base implementation. The purpose is so that you can test the majority of a class but substitute 
    /// out a resource. This is great for testing abstract classes, or avoiding breaking cohesion even 
    /// further with a non-abstract class. 
    /// </summary> 
    /// <typeparam name="T">The instance that you want to build</typeparam> 
    /// <returns>A partial substitute of T</returns> 
    public T CreateSelfSubstitute<T>() where T : class
        => CreateSelfSubstitute<T>(false);

    /// <summary>
    /// Constructs a self-substitute from the services available in the container. A self-substitute is 
    /// a partial substitute (<c>Substitute.ForPartsOf</c>) whose non-configured members invoke the real 
    /// base implementation. The purpose is so that you can test the majority of a class but substitute 
    /// out a resource. This is great for testing abstract classes, or avoiding breaking cohesion even 
    /// further with a non-abstract class. 
    /// </summary> 
    /// <typeparam name="T">The instance that you want to build</typeparam> 
    /// <param name="enablePrivate">When true, non-public constructors will also be used to create instances.</param> 
    /// <returns>A partial substitute of T</returns> 
    public T CreateSelfSubstitute<T>(bool enablePrivate) where T : class
        => (T)BuildSelfSubstitute(typeof(T), enablePrivate);

    /// <summary>
    /// This constructs a self-substitute similar to <see cref="CreateSelfSubstitute{T}(bool)" />.
    /// The created substitute instance is automatically registered using both its implementation and service type.
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <typeparam name="TImplementation">The implementation type</typeparam>
    /// <param name="enablePrivate">When true, non-public constructors will also be used to create instances.</param>
    /// <returns>A partial substitute of the implementation type</returns> 
    public TService WithSelfSubstitute<TService, TImplementation>(bool enablePrivate = false)
        where TImplementation : class, TService
        where TService : class
    {
        return (TService)WithSelfSubstitute(typeof(TService), typeof(TImplementation), enablePrivate);
    }

    /// <summary>
    /// This constructs a self-substitute similar to <see cref="CreateSelfSubstitute{T}(bool)" />.
    /// The created substitute instance is automatically registered using its implementation type.
    /// </summary>
    /// <typeparam name="T">The service type</typeparam>
    /// <param name="enablePrivate">When true, non-public constructors will also be used to create instances.</param>
    /// <returns>A partial substitute of T</returns> 
    public T WithSelfSubstitute<T>(bool enablePrivate = false)
        where T : class
    {
        return (T)WithSelfSubstitute(typeof(T), enablePrivate);
    }

    /// <summary>
    /// This constructs a self-substitute similar to <see cref="CreateSelfSubstitute{T}(bool)" />.
    /// The created substitute instance is automatically registered using both its implementation and service type.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    /// <param name="enablePrivate">When true, non-public constructors will also be used to create instances.</param>
    /// <returns>A partial substitute of the implementation type</returns> 
    public object WithSelfSubstitute(
        Type serviceType,
        Type implementationType,
        bool enablePrivate = false)
    {
        if (serviceType is null) throw new ArgumentNullException(nameof(serviceType));
        if (implementationType is null) throw new ArgumentNullException(nameof(implementationType));
        if (!serviceType.IsAssignableFrom(implementationType))
        {
            throw new ArgumentException($"{implementationType} is not assignable to {serviceType}", nameof(implementationType));
        }

        object selfSubstitute = BuildSelfSubstitute(implementationType, enablePrivate);
        SubstituteInstance instance = new(selfSubstitute);
        WithTypeMap(typeMap =>
        {
            typeMap[implementationType] = instance;
            typeMap[serviceType] = instance;
        });
        return selfSubstitute;
    }

    /// <summary>
    /// This constructs a self-substitute similar to <see cref="CreateSelfSubstitute{T}(bool)" />.
    /// The created substitute instance is automatically registered using its implementation type.
    /// </summary>
    /// <param name="implementationType">The implementation type of the service.</param>
    /// <param name="enablePrivate">When true, non-public constructors will also be used to create instances.</param>
    /// <returns>A partial substitute of the implementation type</returns> 
    public object WithSelfSubstitute(
        Type implementationType,
        bool enablePrivate = false)
    {
        if (implementationType is null) throw new ArgumentNullException(nameof(implementationType));

        object selfSubstitute = BuildSelfSubstitute(implementationType, enablePrivate);
        WithTypeMap(typeMap => typeMap[implementationType] = new SubstituteInstance(selfSubstitute));
        return selfSubstitute;
    }

    private object BuildSelfSubstitute(Type serviceType, bool enablePrivate)
    {
        var context = new ObjectGraphContext(enablePrivate);
        return CreateSubstitute(serviceType, callBase: true, context)
            ?? throw new ObjectCreationException($"Failed to create self substitute of type {serviceType.FullName}", context.DiagnosticMessages);
    }

    #endregion CreateSelfSubstitute

    #region Use

    /// <summary>
    /// Adds an instance to the container.
    /// </summary>
    /// <typeparam name="TService">The type that the instance will be registered as</typeparam>
    /// <param name="service"></param>
    /// <returns>Itself</returns>
    public AutoMocker Use<TService>(TService? service)
        => Use(typeof(TService), service);

    /// <summary>
    /// Adds an instance to the container.
    /// </summary>
    /// <param name="type">The type of service to use</param>
    /// <param name="service">The service to use</param>
    /// <returns>Itself</returns>
    public AutoMocker Use(Type type, object? service)
    {
        if (type is null) throw new ArgumentNullException(nameof(type));
        if (service != null && !type.IsInstanceOfType(service))
        {
            throw new ArgumentException($"{nameof(service)} is not of type {type}", nameof(service));
        }
        WithTypeMap(typeMap =>
        {
            if (typeMap.TryGetValue(type, out IInstance existingInstance) &&
                ReferenceEquals(existingInstance.Value, service))
            {
                throw new InvalidOperationException($"The service instance has already been added. You can safely remove this call to {nameof(AutoMocker)}.{nameof(Use)}");
            }
            typeMap[type] = service is { } value && IsSubstitute(value)
                ? new SubstituteInstance(value)
                : new RealInstance(service);
        });

        return this;
    }

    /// <summary>
    /// Adds a callback delegate to the container.
    /// This delegate will be invoked when the service type is first requested.
    /// The resulting value will be cached.
    /// </summary>
    /// <typeparam name="TService">The type that the instance will be registered as</typeparam>
    /// <param name="factory">The factory callback.</param>
    /// <returns>Itself</returns>
    /// <exception cref="ArgumentNullException">When the factory is null.</exception>
    public AutoMocker Use<TService>(Func<AutoMocker, TService> factory)
        where TService : class
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));

        CallbackResolver resolver = Resolvers.OfType<CallbackResolver>().FirstOrDefault()
            ?? throw new InvalidOperationException($"The {nameof(CallbackResolver)} must be a registered resolver.");

        resolver.AddCallback(factory);
        return this;
    }

    /// <summary>
    /// Adds a callback delegate to the container.
    /// This delegate will be invoked when the service type is first requested.
    /// The resulting value will be cached.
    /// </summary>
    /// <typeparam name="TService">The type that the instance will be registered as</typeparam>
    /// <param name="factory">The factory callback.</param>
    /// <returns>Itself</returns>
    /// <exception cref="ArgumentNullException">When the factory is null.</exception>
    public AutoMocker Use<TService>(Func<TService> factory)
        where TService : class
    {
        return Use(_ => factory());
    }

    /// <summary>
    /// Adds a callback delegate to the container.
    /// This delegate will be invoked when the service type is first requested.
    /// The resulting value will be cached.
    /// </summary>
    /// <typeparam name="TService">The type that the instance will be registered as</typeparam>
    /// <typeparam name="TImplementation">The service implementation type</typeparam>
    /// <returns>Itself</returns>
    public AutoMocker Use<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        return Use<TService>(mocker => mocker.Get<TImplementation>());
    }

    /// <summary>
    /// Creates an instance of <typeparamref name="TImplementation"/> and registers it as for service type <typeparamref name="TService"/>.
    /// This is a convenience method for Use&lt;<typeparamref name="TService"/>&gt;(CreateInstance&lt;<typeparamref name="TImplementation"/>&gt;())
    /// </summary>
    /// <typeparam name="TService">The service type</typeparam>
    /// <typeparam name="TImplementation">The service implementation type</typeparam>
    /// <returns>The created instance</returns>
    public TImplementation With<TService, TImplementation>()
        where TImplementation : class, TService
    {
        TImplementation instance = CreateInstance<TImplementation>();
        Use<TService>(instance);
        return instance;
    }

    /// <summary>
    /// Creates an instance of <typeparamref name="TImplementation"/> and registers it as for service type <typeparamref name="TImplementation"/>.
    /// This is a convenience method for Use&lt;<typeparamref name="TImplementation"/>&gt;(CreateInstance&lt;<typeparamref name="TImplementation"/>&gt;())
    /// </summary>
    /// <typeparam name="TImplementation">The service implementation type</typeparam>
    /// <returns>The created instance</returns>
    public TImplementation With<TImplementation>()
        where TImplementation : class
    {
        TImplementation instance = CreateInstance<TImplementation>();
        Use(instance);
        return instance;
    }

    /// <summary>
    /// Creates an instance of <paramref name="implementationType"/> and registers it for service type <paramref name="serviceType"/>.
    /// This is a convenience method for Use(<paramref name="serviceType"/>, CreateInstance(<paramref name="implementationType"/>))
    /// </summary>
    /// <returns>The created instance</returns>
    public object With(Type serviceType, Type implementationType)
    {
        object instance = CreateInstance(implementationType);
        Use(serviceType, instance);
        return instance;
    }

    /// <summary>
    /// Creates an instance of <paramref name="implementationType"/> and registers it for service type <paramref name="implementationType"/>.
    /// This is a convenience method for Use(<paramref name="implementationType"/>, CreateInstance(<paramref name="implementationType"/>))
    /// </summary>
    /// <returns>The created instance</returns>
    public object With(Type implementationType)
    {
        object instance = CreateInstance(implementationType);
        Use(implementationType, instance);
        return instance;
    }

    #endregion Use

    #region Get

    /// <summary>
    /// Searches and retrieves an object from the container that matches TService. This can be
    /// a service setup explicitly via `.Use()` or implicitly with `.CreateInstance()`.
    /// </summary>
    /// <typeparam name="TService">The class or interface to search on</typeparam>
    /// <returns>The object that implements TService</returns>
    public TService Get<TService>()
    {
        if (Get(typeof(TService)) is TService service)
            return service;

        return default!;
    }

    /// <summary>
    /// Searches and retrieves an object from the container that matches TService. This can be
    /// a service setup explicitly via `.Use()` or implicitly with `.CreateInstance()`.
    /// </summary>
    /// <typeparam name="TService">The class or interface to search on</typeparam>
    /// <param name="enablePrivate">When true, non-public constructors will also be used to create instances.</param>
    /// <returns>The object that implements TService</returns>
    public TService Get<TService>(bool enablePrivate)
    {
        if (Get(typeof(TService), enablePrivate) is TService service)
            return service;

        return default!;
    }

    /// <summary>
    /// Searches and retrieves an object from the container that matches the serviceType. This can be
    /// a service setup explicitly via `.Use()` or implicitly with `.CreateInstance()`.
    /// </summary>
    /// <param name="serviceType">The type of service to retrieve</param>
    /// <returns></returns>
    public object Get(Type serviceType)
    {
        return Get(serviceType, enablePrivate: false);
    }

    /// <summary>
    /// Searches and retrieves an object from the container that matches the serviceType. This can be
    /// a service setup explicitly via `.Use()` or implicitly with `.CreateInstance()`.
    /// </summary>
    /// <param name="serviceType">The type of service to retrieve</param>
    /// <param name="enablePrivate">When true, non-public constructors will also be used to create instances.</param>
    /// <returns></returns>
    public object Get(Type serviceType, bool enablePrivate)
    {
        return Get(serviceType, new ObjectGraphContext(enablePrivate));
    }

    private object Get(Type serviceType, ObjectGraphContext context)
    {
        if (TryGet(serviceType, context, out IInstance? service, out bool noCache))
        {
            if (!noCache && TypeMap is { } typeMap && !typeMap.ContainsKey(serviceType))
            {
                typeMap[serviceType] = service;
            }
            return service.Value!; //Should generally not be null, unless the caller has forced a null in with Use
        }
        throw new ArgumentException($"{serviceType} could not resolve to an object.", nameof(serviceType));
    }

    internal bool TryGet(
        Type serviceType,
        ObjectGraphContext context,
        [NotNullWhen(true)] out IInstance? service,
        out bool noCache)
    {
        if (serviceType is null) throw new ArgumentNullException(nameof(serviceType));

        if (TryResolve(serviceType, context, out IInstance? instance, out noCache))
        {
            service = instance;
            return true;
        }
        service = null;
        return false;
    }

    /// <inheritdoc />
    object? IServiceProvider.GetService(Type serviceType)
    {
        if (TryGet(serviceType, new ObjectGraphContext(false), out IInstance? service, out bool noCache))
        {
            if (!noCache && TypeMap is { } typeMap && !typeMap.ContainsKey(serviceType))
            {
                typeMap[serviceType] = service;
            }
            return service.Value;
        }
        return null;
    }

    #endregion Get

    #region GetSubstitute

    /// <summary>
    /// Searches and retrieves the substitute that the container uses for TService.
    /// </summary>
    /// <typeparam name="TService">The class or interface to search on</typeparam>
    /// <exception cref="ArgumentException">if the requested object wasn't a substitute</exception>
    /// <returns>A substitute of TService</returns>
    public TService GetSubstitute<TService>() where TService : class
        => (TService)GetSubstitute(typeof(TService));

    /// <summary>
    /// Searches and retrieves the substitute that the container uses for TService.
    /// </summary>
    /// <typeparam name="TService">The class or interface to search on</typeparam>
    /// <param name="enablePrivate">When true, non-public constructors will also be used to create instances.</param>
    /// <exception cref="ArgumentException">if the requested object wasn't a substitute</exception>
    /// <returns>A substitute of TService</returns>
    public TService GetSubstitute<TService>(bool enablePrivate) where TService : class
        => (TService)GetSubstitute(typeof(TService), enablePrivate);

    /// <summary>
    /// Searches and retrieves the substitute that the container uses for serviceType.
    /// </summary>
    /// <param name="serviceType">The type of service to retrieve</param>
    /// <returns>A substitute of serviceType</returns>
    public object GetSubstitute(Type serviceType)
    {
        if (serviceType is null) throw new ArgumentNullException(nameof(serviceType));

        return GetSubstitute(serviceType, enablePrivate: false);
    }

    /// <summary>
    /// Searches and retrieves the substitute that the container uses for serviceType.
    /// </summary>
    /// <param name="serviceType">The type of service to retrieve</param>
    /// <param name="enablePrivate">When true, non-public constructors will also be used to create instances.</param>
    /// <returns>A substitute of serviceType</returns>
    public object GetSubstitute(Type serviceType, bool enablePrivate)
    {
        if (serviceType is null) throw new ArgumentNullException(nameof(serviceType));

        return GetSubstituteImplementation(serviceType, enablePrivate);
    }

    private object GetSubstituteImplementation(Type serviceType, bool enablePrivate)
    {
        if (TryResolve(serviceType, new ObjectGraphContext(enablePrivate, isSubstituteCreation: true), out IInstance? instance, out bool noCache) &&
            instance.IsSubstitute)
        {
            if (!noCache && TypeMap is { } typeMap && !typeMap.ContainsKey(serviceType))
            {
                typeMap[serviceType] = instance;
            }
            return instance.Value!;
        }
        throw new ArgumentException($"Registered service `{Get(serviceType)?.GetType()}` was not a substitute");
    }

    #endregion GetSubstitute

    #region Combine

    /// <summary>
    /// Combines all given types so that they are substituted by the same
    /// substitute instance. Some IoC containers call this "Forwarding" one type to 
    /// other interfaces. In the end, this just means that all given
    /// types will be implemented by the same instance.
    /// </summary>
    /// <remarks>
    /// Because NSubstitute does not support adding interfaces to an existing substitute,
    /// this creates a new substitute implementing all of the given types, replacing any
    /// previously cached substitutes for these types. At most one of the types may be a class.
    /// </remarks>
    public void Combine(Type type, params Type[] forwardTo)
    {
        if (type is null) throw new ArgumentNullException(nameof(type));

        Type[] serviceTypes = [type, .. forwardTo];
        ObjectGraphContext context = new(false);
        object substitute = CreateSubstitute(serviceTypes, CallBase, context)
            ?? throw new ObjectCreationException(
                $"Unable to create a substitute implementing {string.Join(", ", serviceTypes.Select(t => t.FullName))}",
                context.DiagnosticMessages);

        SubstituteInstance instance = new(substitute);
        WithTypeMap(typeMap =>
        {
            foreach (var serviceType in serviceTypes)
            {
                typeMap[serviceType] = instance;
            }
        });
    }

    #endregion Combine

    #region Cleanup

    /// <summary>
    /// Retrieve an IDisposable instance that will dispose of all disposable
    /// instances contained within this AutoMocker instance.
    /// </summary>
    /// <returns></returns>
    public IDisposable AsDisposable() => Get<IAutoMockerDisposable>();

    #endregion Cleanup

    #region Utilities

    /// <summary>
    /// Determines whether the given object is an NSubstitute substitute.
    /// </summary>
    /// <param name="value">The object to check.</param>
    /// <returns>True if the object is a substitute.</returns>
    public static bool IsSubstitute([NotNullWhen(true)] object? value)
        => value is ICallRouterProvider ||
           (value is Delegate { Target: ICallRouterProvider });

    internal object? CreateSubstitute(Type serviceType, bool callBase, ObjectGraphContext objectGraphContext)
        => CreateSubstitute([serviceType], callBase, objectGraphContext);

    private object? CreateSubstitute(Type[] serviceTypes, bool callBase, ObjectGraphContext objectGraphContext)
    {
        if (serviceTypes.Any(serviceType => !serviceType.IsMockable()))
        {
            return null;
        }

        Type? classType = serviceTypes.FirstOrDefault(serviceType
            => serviceType.IsClass && !typeof(Delegate).IsAssignableFrom(serviceType));

        object?[] constructorArgs = [];
        if (classType is not null &&
            TryGetConstructorInvocation(classType, objectGraphContext, out _, out IInstance[]? arguments))
        {
            constructorArgs = [.. arguments.Select(x => x.Value)];
        }

        try
        {
            ISubstituteFactory factory = SubstitutionContext.Current.SubstituteFactory;
            return callBase && classType is not null
                ? factory.CreatePartial(serviceTypes, constructorArgs)
                : factory.Create(serviceTypes, constructorArgs);
        }
        catch (Exception ex)
        {
            objectGraphContext.AddDiagnosticMessage($"Failed to create substitute for {string.Join(", ", serviceTypes.Select(t => t.AssemblyQualifiedName))} {ex}");
            return null;
        }
    }

    internal bool TryGetConstructorInvocation(
        Type type,
        ObjectGraphContext context,
        [NotNullWhen(true)] out ConstructorInfo? constructor,
        [NotNullWhen(true)] out IInstance[]? arguments)
    {
        IEnumerable<ConstructorInfo> constructors = type
            .GetConstructors(context.BindingFlags)
            .OrderByDescending(x => x.GetParameters().Length)
            .Concat([Empty(type)])
            .Where(x => x is not null)!;

        context.VisitedTypes.Add(type);
        foreach (var ctor in constructors)
        {
            if (TryCreateArguments(ctor, context, out IInstance[] args))
            {
                constructor = ctor;
                arguments = args;
                return true;
            }
        }
        constructor = null;
        arguments = null;
        return false;

        static ConstructorInfo? Empty(Type type) => type
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(x => x.GetParameters().Length is 0);

        bool TryCreateArguments(ConstructorInfo constructor, ObjectGraphContext context, out IInstance[] arguments)
        {
            var parameters = constructor.GetParameters();
            arguments = new IInstance[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                ObjectGraphContext parameterContext = new(context, parameters[i]);
                if (!TryGet(parameters[i].ParameterType, parameterContext, out IInstance? service, out bool noCache))
                {
                    context.AddDiagnosticMessage($"Rejecting constructor {GetConstructorDisplayString(constructor)}, because {nameof(AutoMocker)} was unable to create parameter '{parameters[i].ParameterType.FullName} {parameters[i].Name}'");
                    return false;
                }

                if (!noCache)
                {
                    EnsureCached(parameters[i].ParameterType, service);
                }
                arguments[i] = service;
            }
            return true;
        }

        static string GetConstructorDisplayString(ConstructorInfo constructor)
        {
            StringBuilder sb = new();
            sb.Append(constructor.DeclaringType?.FullName);
            sb.Append("(");
            ParameterInfo[] parameters = constructor.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                sb.Append(parameters[i].ParameterType.FullName);
                sb.Append(' ');
                sb.Append(parameters[i].Name);
                if (i < parameters.Length - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.Append(")");

            return sb.ToString();
        }
    }

    private void EnsureCached(Type type, IInstance instance)
    {
        WithTypeMap(typeMap =>
        {
            if (!typeMap.TryGetValue(type, out _))
            {
                typeMap[type] = instance;
            }
        });
    }

    private void WithTypeMap(Action<NonBlocking.ConcurrentDictionary<Type, IInstance>> onTypeMap)
    {
        if (TypeMap is { } typeMap)
        {
            onTypeMap(typeMap);
        }
        else
        {
            throw new InvalidOperationException($"{nameof(CacheResolver)} was not found. Cannot cache service instance without resolver.");
        }
    }

    #endregion
}
