# FolioTrace Schematic Diagrams

These diagrams are split into marketing-oriented views and technical-review views. They use Mermaid so they can be rendered directly in GitHub-compatible Markdown tools.

## Marketing Diagrams

### Traceable Portfolio Story

```mermaid
flowchart LR
    Client["Client portfolio activity"]
    Capture["FolioTrace captures each fact as an event"]
    Timeline["A durable timeline of decisions and movements"]
    Views["Valuation and audit views at any chosen point"]
    Confidence["Clear answers with traceable evidence"]

    Client --> Capture --> Timeline --> Views --> Confidence

    Capture --> Reference["Reference data"]
    Capture --> Cash["Cash movements"]
    Capture --> Inspecie["In-specie movements"]
    Capture --> Cancellations["Controlled cancellations"]
```

### Product Value Map

```mermaid
flowchart TB
    FolioTrace["FolioTrace"]

    FolioTrace --> Time["Time-aware records"]
    FolioTrace --> Audit["Audit-ready history"]
    FolioTrace --> Holdings["Holding and position clarity"]
    FolioTrace --> Operations["Operational workflows"]

    Time --> TimeA["Valuation date"]
    Time --> TimeB["Audit as-at date"]

    Audit --> AuditA["Immutable event trail"]
    Audit --> AuditB["Cancelled on timestamps"]

    Holdings --> HoldingsA["Cash investable holdings"]
    Holdings --> HoldingsB["Instrument positions"]

    Operations --> OpsA["Cash in and cash out"]
    Operations --> OpsB["Inspecie in and out"]
    Operations --> OpsC["Set-level cancellation"]
```

### Transaction Set Narrative

```mermaid
journey
    title One Account Transaction Set
    section Create
      Select account: 5: User
      Choose movement type: 5: User
      Pick holding or instrument: 4: User
      Submit balanced set: 5: System
    section Trace
      Store credit and debit events: 5: System
      Show set card on account: 4: User
      Include in account subtotals: 4: System
    section Correct
      Cancel whole set if needed: 4: User
      Preserve original movements: 5: System
      Show cancelled on audit time: 5: User
```

## Technical Review Diagrams

### Runtime Architecture

```mermaid
flowchart LR
    UI["SvelteKit UI"]
    API["ASP.NET Minimal API"]
    Services["Feature services"]
    Cache["In-memory event repository"]
    Marten["Marten event store"]
    Aggregates["Event-built aggregates"]

    UI -->|"GET aggregates and events"| API
    UI -->|"POST event requests"| API
    API --> Services
    API --> Cache
    Services --> Cache
    Cache --> Marten
    Cache --> Aggregates

    Aggregates --> Accounts["Accounts"]
    Aggregates --> Holdings["Holdings"]
    Aggregates --> Positions["Holding positions"]
    Aggregates --> Instruments["Instruments"]
```

### Transaction Cancellation Flow

```mermaid
sequenceDiagram
    participant U as User
    participant UI as Accounts page
    participant API as Transaction API
    participant B as TransactionCancellationEventBuilder
    participant Store as Event repository
    participant Cache as Aggregate invalidation

    U->>UI: Click set-level X
    UI->>API: POST /Events/Transaction/Cancel
    API->>Store: Load transaction stream
    API->>B: Create cancellation for EventSetID
    B->>B: Find original movement set
    B->>B: Validate not already cancelled
    B->>B: Validate one AccountID
    B-->>API: Cancellation events
    API->>Store: Append cancellation events
    API->>Cache: Invalidate affected aggregates
    API-->>UI: Accepted event IDs
    UI-->>U: Show struck set and Cancelled on timestamp
```

### Transaction Set Rules

```mermaid
classDiagram
    class TransactionSet {
      EventSetID
      AccountID
      EventDateTime
      SettlementDateTime
      Reason
    }

    class TransactionCreditEvent {
      EventID
      EventIDGroup
      HoldingID
      InstrumentID
      Quantity
      BookCost
    }

    class TransactionDebitEvent {
      EventID
      EventIDGroup
      HoldingID
      InstrumentID
      Quantity
      BookCost
    }

    class TransactionCancellationEvent {
      EventID
      EventSetID
      AccountID
      CancelledEventID
      CancelledIDGroup
      AuditDateTime
    }

    TransactionSet "1" --> "1..*" TransactionCreditEvent : credits
    TransactionSet "1" --> "1..*" TransactionDebitEvent : debits
    TransactionCancellationEvent "1..*" --> "1" TransactionSet : cancels original set
```

### Account Page Transaction View

```mermaid
flowchart TB
    Events["Transaction events"]
    Filter["Filter by account, valuation date, audit as-at"]
    Group["Group movement events by EventSetID"]
    Match["Match cancellations by AccountID and CancelledIDGroup"]
    Cards["Render transaction set cards"]
    Active["Active cards show X cancel action"]
    Cancelled["Cancelled cards are struck and show Cancelled on"]
    Totals["Subtotals count active movement sets only"]

    Events --> Filter --> Group --> Match --> Cards
    Cards --> Active
    Cards --> Cancelled
    Group --> Totals
    Match --> Totals
```

### Event Time Model

```mermaid
flowchart LR
    EventDate["EventDateTime: when the business fact applies"]
    AuditDate["AuditDateTime: when the system learned it"]
    Query["Query context"]
    Projection["Aggregate or account view"]

    Query --> Valuation["Valuation date filter"]
    Query --> AsAt["Audit as-at filter"]

    EventDate --> Valuation
    AuditDate --> AsAt
    Valuation --> Projection
    AsAt --> Projection

    Projection --> Answer["State as it looked then, using facts known then"]
```
