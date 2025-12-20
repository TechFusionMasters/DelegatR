# MediatorKit

MediatorKit is a minimal, in-process mediator for .NET.

It provides a small set of contracts plus a single default runtime `Mediator` implementation that can dispatch requests and publish notifications. Resolution is **DI-agnostic** via a `ServiceFactory` delegate.

## V1 scope

- `Send`:
  - Dispatch a request to exactly one handler
  - Optional pipeline behaviors that wrap the handler
  - Cancellation token flows through behaviors and handler
- `Publish`:
  - Resolve all handlers for the concrete notification type
  - Execute sequentially in resolver order
  - Stop on the first exception
- `ServiceFactory`:
  - A delegate-based abstraction for resolving services from any container (or your own resolver)

## Out of scope (V1)

- Streaming requests / async enumerable responses
- Advanced publish strategies (parallelism, exception aggregation, fire-and-forget)
- Assembly scanning / auto-registration helpers
- Built-in logging/metrics (use pipeline behaviors instead)
- Licensing or license enforcement

## Conceptual usage (high level)

1. Define a request type that implements `IRequest<TResponse>`.
2. Implement a handler for it using `IRequestHandler<TRequest, TResponse>`.
3. (Optional) Register one or more `IPipelineBehavior<TRequest, TResponse>` to wrap `Send`.
4. Provide a `ServiceFactory` that can resolve:
   - The request handler for the concrete request type
   - The pipeline behavior sequence for that request type
   - The notification handlers sequence for the concrete notification type
5. Create `new Mediator(factory)` and call `Send` / `Publish`.

## Packaging

The library is designed to be packed as a NuGet package from `src/MediatorKit`.

## License

MIT is recommended for this repository (add a `LICENSE` file when ready).
