# Why MediatorKit

MediatorKit exists to provide a small, predictable in-process mediator for .NET applications.

## What problem it solves

Many applications need a simple way to:

- Decouple callers from concrete handlers (request/response dispatch)
- Orchestrate application workflows without tightly coupling layers
- Apply cross-cutting concerns consistently (validation, logging, timing, retries) without scattering code across handlers

MediatorKit provides a minimal set of contracts and a single default runtime implementation that supports these needs without requiring a specific dependency injection container.

## Core guarantees (V1)

MediatorKit is designed around a few explicit execution rules:

- **Send uses exactly one handler**
  - A request is dispatched to a single handler for the concrete request type.
- **Publish is sequential and predictable**
  - Notification handlers are invoked one-by-one in the order returned by the configured resolver.
  - Execution stops immediately on the first exception (the exception is propagated).
  - If no handlers are registered, publish is a successful no-op.
- **Pipeline behaviors are ordered**
  - Pipeline behaviors wrap request handling in the same order they are returned by the configured resolver.
  - Behaviors may wrap, augment, or short-circuit the request.
- **Cancellation token propagation**
  - The provided `CancellationToken` is passed through the full call chain (behaviors + handlers).

These guarantees are intended to make behavior easy to reason about and easy to test.

## What V1 intentionally excludes (and why)

V1 keeps the surface area small and avoids features that typically increase complexity or introduce ambiguous behavior:

- **Streaming requests / async enumerable results**
  - Deferred to future versions to keep the initial execution model straightforward.
- **Advanced publish strategies** (parallelism, aggregation, “continue on error”, fire-and-forget)
  - Excluded to preserve deterministic ordering and failure behavior.
- **Assembly scanning / auto-registration**
  - Excluded to keep the runtime container-agnostic and avoid hidden conventions.

## Who it’s for

MediatorKit is a fit when you want:

- Enterprise applications that value predictable, testable orchestration
- Internal platforms that need a stable, minimal contract layer
- OSS projects that want a small mediator with explicit execution rules
- Teams that prefer container-agnostic integration via a simple resolver delegate

It may not be a fit if you require rich built-in registration/scanning features or advanced publish models in the core runtime.

## Clean-room note

MediatorKit’s design and implementation in this repository are based on the behavior and requirements described in the project’s own documentation and tests.
