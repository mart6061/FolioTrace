# FolioTrace Application Overview

FolioTrace appears to be an event-sourced domain application for modelling financial reference data and, eventually, portfolio or instrument-related state over time.

The core idea is that domain facts are captured as immutable events. Aggregates such as countries and currencies are rebuilt from those events for a requested valuation date and audit/as-at date. This lets the application answer questions like "what did the world look like at this valuation time, using only information known by this audit time?"

## Current Shape

The solution is split into a few early layers:

- `Shared` contains domain types, value objects, events, aggregates, builders, and common contracts.
- `Repository` owns persistence and event access.
- `Services` is intended to become the application-facing source for aggregates and entities.
- `UIConsole` is currently a simple operational entry point for running initialization.
- `API` is scaffolded as a future HTTP entry point.
- `Test` is present for automated tests.

## Domain Model

Domain values are wrapped in explicit value objects such as `EventDateTime`, `AuditDateTime`, `Alpha2`, `Alpha3`, and other financial/reference-data identifiers. These types validate their own invariants instead of letting raw strings, dates, or identifiers move freely through the system.

Events implement `IEventBase` through `EventBase`. Examples include:

- `CountryCreatedEvent`
- `CountryModifiedEvent`
- `CurrencyCreatedEvent`
- `CurrencyModifiedEvent`

Aggregates such as `Countries` are rebuilt by applying the relevant event stream. The aggregate constructor currently filters events by valuation date and audit/as-at date, then applies creation and modification events in order.

Aggregates also carry provenance metadata, including the `LastEventID` and `LastAuditDateTime` of the state they represent.

## Event Storage

Marten is treated as the durable event store. Domain events are written to Marten's event-store tables rather than being stored as separate document tables.

At runtime, the app-facing repository is `InMemoryEventsRepository`. It is backed by `MartenEventRepository`:

- On startup, hosted apps warm the in-memory cache from Marten.
- Reads use the in-memory stream and event indexes.
- Writes go to Marten first.
- After a successful durable write, the event is added to the in-memory cache.

This keeps Marten as the source of durable truth while allowing the app to explore simple and fast in-memory aggregate rebuilds.

## Initialization

`InitRepository` currently clears existing event data and creates setup events for country reference data. These country setup events are written to a deterministic countries stream using `Constants.Initialisation.CountriesStreamId`.

The setup reason and setup timestamps live under `Constants.Initialisation`.

## Services Layer

The `Services` project is intended to become the application-facing layer for aggregate and entity access.

`CountryService` currently exposes a `Get` method that rebuilds the `Countries` aggregate from country events:

- `Get(EventDateTime valuationDate)` uses `Constants.Valuation.All` as the audit/as-at date.
- `Get(EventDateTime valuationDate, AuditDateTime asAt)` rebuilds the aggregate for explicit valuation and audit/as-at dates.

Over time, this layer can become the main source for application use cases, hiding event storage and aggregate rebuild mechanics from UI and API code.

## Current Direction

The architecture is moving toward:

1. Domain events as the durable record of truth.
2. Marten for persistence.
3. In-memory event access for runtime simplicity and performance exploration.
4. Services as the app-facing source for aggregates/entities.
5. Console/API projects as thin entry points over the service layer.

The application is still early, but the emerging design is a clean event-sourced core with explicit domain types and a pragmatic in-memory read path.
