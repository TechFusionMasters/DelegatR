# Migration notes (conceptual)

This document provides conceptual guidance for teams considering a move to MediatorKit.

It is not a drop-in compatibility guide. The intent is to help you align your application architecture and expectations with MediatorKit’s explicit execution rules.

## Core concept mapping

- **Request → Handler**
  - A request represents a single operation that returns a response.
  - A handler is the one component responsible for producing that response.

- **Notification → Handlers**
  - A notification represents an event that may have multiple independent handlers.
  - Each handler can perform side effects (logging, cache invalidation, downstream calls, etc.).

- **Pipeline → Behaviors**
  - Pipeline behaviors provide a structured way to add cross-cutting concerns around request handling.
  - Examples include timing, validation, retries, and correlation/trace context.

## Behavioral guarantees to plan around

MediatorKit’s runtime is intentionally strict and predictable:

- **Send uses a single handler**
  - Each request is dispatched to exactly one handler for the concrete request type.

- **Publish is sequential and stops on first failure**
  - Notification handlers execute **one-by-one** in resolver order.
  - If any handler throws, subsequent handlers are not invoked and the exception is propagated.

- **Pipeline ordering is resolver ordering**
  - Behaviors execute in the order they are returned by your configured resolver.
  - Behaviors can wrap and/or short-circuit the request by choosing whether to call the `next` delegate.

- **Cancellation propagation**
  - The `CancellationToken` supplied to `Send`/`Publish` is passed through the full call chain.

## What V1 does not include (and how to adapt)

MediatorKit V1 intentionally excludes some capabilities to keep execution predictable and the surface area small:

- **No streaming request model**
  - If you need streaming results, consider modeling streaming as a separate abstraction in your application layer.

- **No advanced publish strategies**
  - There is no built-in parallel publish, exception aggregation, or “continue on error”.
  - If your system depends on those strategies, you may need to redesign the notification handling approach (e.g., fan-out to a queue) or accept sequential semantics.

- **No assembly scanning / auto-registration**
  - You are responsible for registering handlers/behaviors in your container and providing a resolver.
  - This tends to make dependencies explicit and reduces hidden conventions.

## Mindset shifts that help migrations

- **Make resolution explicit**
  - MediatorKit expects you to provide a resolver (`ServiceFactory`) that returns the specific handler and the behavior/handler sequences.

- **Treat publish ordering as deterministic**
  - Because publish is sequential, ordering becomes observable; design handlers to be independent whenever possible.

- **Prefer pipeline behaviors for cross-cutting concerns**
  - Keep handlers focused on business logic; put generic concerns into behaviors.

## Who should (and should not) migrate

A migration may be a good fit if you want:

- A minimal mediator with explicit, testable execution rules
- Deterministic sequential publish semantics
- Container-agnostic integration via a small resolver delegate

It may not be a good fit if you require:

- Built-in auto-registration/scanning
- Streaming as a first-class request shape
- Rich publish modes (parallelism, aggregation, complex failure policies) in the core runtime
