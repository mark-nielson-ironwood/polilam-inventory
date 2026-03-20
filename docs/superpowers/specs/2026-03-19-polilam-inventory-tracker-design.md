# Polilam Inventory Tracker — Design Specification

## Overview

A web application for tracking sheet inventory of Polilam materials (compact laminate and plastic laminate). The system records purchase orders, shipment receipts (including partial fulfillments), inventory additions, and sheet pulls (actual and scheduled). It produces reports for internal inventory management and for Polilam billing.

**Users:** Small purchasing team (2–3 people) on a company LAN. No authentication or role-based access required.

**Platform:** ASP.NET Core MVC with Razor Pages, SQLite database, Bootstrap 5 UI. Hosted on a Windows server on the internal network via IIS or Windows Service.

---

## Domain Concepts

### Materials

All materials are manufactured by Polilam. Each sheet is identified by:

- **Pattern** — a named laminate design (e.g., "Black 454", "Espresso", "Nebraska")
- **Width** — sheet width in inches (e.g., 30, 60)
- **Length** — sheet length in inches (e.g., 72, 144)
- **Thickness** — sheet thickness in inches (e.g., 0.039, 0.25, 0.5, 0.75, 1)

**Material type** is derived from thickness:
- Thickness = 0.039 → Plastic Laminate
- All other thicknesses → Compact Laminate (Phenolic)

A unique inventory position is the combination of Pattern + Width + Length + Thickness.

### Billing Model

Polilam does not charge when sheets are delivered into inventory. They charge when sheets are pulled (used). The company must submit periodic removal reports to Polilam showing how many sheets were pulled in a given time period.

---

## Data Model

### Patterns

| Field          | Type    | Notes                                      |
|----------------|---------|---------------------------------------------|
| Id             | int     | Primary key                                 |
| Name           | string  | e.g., "Black 454", "Espresso"               |
| ReorderTrigger | int     | Threshold for reorder alert (default: 5)    |

### Sizes

| Field     | Type    | Notes                                      |
|-----------|---------|---------------------------------------------|
| Id        | int     | Primary key                                 |
| Width     | decimal | Sheet width in inches                       |
| Length    | decimal | Sheet length in inches                      |
| Thickness | decimal | Sheet thickness in inches                   |

Derived property: `MaterialType` — "Plastic Laminate" if Thickness = 0.039, otherwise "Compact Laminate".

### Orders

A purchase order placed with Polilam.

| Field          | Type     | Notes                                   |
|----------------|----------|------------------------------------------|
| Id             | int      | Primary key                              |
| PatternId      | int      | FK → Patterns                            |
| SizeId         | int      | FK → Sizes                               |
| QuantityOrdered| int      | Number of sheets ordered                 |
| OrderDate      | date     | Date order was placed                    |
| EtaDate        | date     | Expected arrival date                    |
| PoNumber       | string   | Purchase order number                    |
| Note           | string?  | Optional                                 |

Derived properties:
- `QuantityReceived` — sum of linked Receipts
- `QuantityOutstanding` — QuantityOrdered − QuantityReceived
- `IsFilled` — true when QuantityOutstanding = 0

### Receipts

A shipment received against an order. Multiple receipts per order are allowed, enabling clean partial fulfillment tracking without row splitting.

| Field          | Type     | Notes                                   |
|----------------|----------|------------------------------------------|
| Id             | int      | Primary key                              |
| OrderId        | int      | FK → Orders                              |
| QuantityReceived| int     | Sheets received in this shipment         |
| DateReceived   | date     | Date received                            |
| Note           | string?  | Optional                                 |

**Validation:** QuantityReceived must not exceed the order's QuantityOutstanding.

### InventoryAdjustments

Initial stock entries and manual corrections.

| Field          | Type     | Notes                                   |
|----------------|----------|------------------------------------------|
| Id             | int      | Primary key                              |
| PatternId      | int      | FK → Patterns                            |
| SizeId         | int      | FK → Sizes                               |
| QuantityAdded  | int      | Sheets added                             |
| DateAdded      | date     | Date of adjustment                       |
| Note           | string?  | Optional                                 |

### PlannedClaims

Future scheduled pulls — material reserved for a known upcoming job.

| Field          | Type     | Notes                                   |
|----------------|----------|------------------------------------------|
| Id             | int      | Primary key                              |
| PatternId      | int      | FK → Patterns                            |
| SizeId         | int      | FK → Sizes                               |
| Quantity       | int      | Sheets claimed                           |
| ScheduledDate  | date     | Date material will be needed             |
| SoNumber       | string   | Sales order / job number                 |
| Note           | string?  | Optional                                 |

**Auto-conversion:** When ScheduledDate ≤ today, the system automatically converts the PlannedClaim into an ActualPull (copies fields, deletes the PlannedClaim). This runs on application startup and on each page load.

### ActualPulls

Material that has physically left inventory.

| Field          | Type     | Notes                                   |
|----------------|----------|------------------------------------------|
| Id             | int      | Primary key                              |
| PatternId      | int      | FK → Patterns                            |
| SizeId         | int      | FK → Sizes                               |
| Quantity       | int      | Sheets pulled                            |
| PullDate       | date     | Date pulled                              |
| SoNumber       | string   | Sales order / job number                 |
| Note           | string?  | Optional                                 |

**Validation:** For immediate pulls ("Pull Now"), Quantity must not exceed current inventory for that Pattern+Size. PlannedClaims may exceed current inventory (they are forward-looking) but will show a deficit warning.

---

## Inventory Calculation

Inventory is never stored as a persisted number. It is always computed:

```
Current Inventory (for a Pattern+Size) =
    SUM(InventoryAdjustments.QuantityAdded)
  + SUM(Receipts.QuantityReceived)      -- via Orders matching this Pattern+Size
  - SUM(ActualPulls.Quantity)
```

This makes the system audit-proof — the inventory count is always derivable from the transaction history.

### Projected Inventory Calculations

For each Pattern+Size that has open orders:

1. **Committed Before Arrival** = SUM of PlannedClaims where ScheduledDate < earliest open order's EtaDate
2. **Projected at Arrival** = Current Inventory − Committed Before Arrival
3. **Total Committed** = SUM of all PlannedClaims (regardless of date)
4. **Projected Balance** = Current Inventory + QuantityOutstanding (all open orders) − Total Committed

### Alerts and Warnings

- **Projected at Arrival < 0** → Red highlight. Deficit before ordered material arrives — purchasing needs to act.
- **Projected Balance < 0** → Red highlight. Even after all orders arrive, there won't be enough stock.
- **Projected Balance ≤ ReorderTrigger** → Amber "Re-order may be needed" warning.
- **Adding a PlannedClaim that would push Projected Balance negative** → Amber warning on the form (non-blocking). The user can still save because they may know an order is coming but haven't entered it yet.

---

## Application Pages

### Navigation

Dark sidebar with these sections:
- Dashboard (home)
- Transactions: Place Order, Receive Shipment, Pull Sheets
- Reports: Inventory Report, Removal Report, Transaction Report
- Settings

### Dashboard

The landing page. Shows at-a-glance inventory health.

**Alert Banner** (top of page):
- Lists all deficit situations (Projected at Arrival < 0 or Projected Balance < 0) in red
- Lists all reorder warnings (Projected Balance ≤ ReorderTrigger) in amber
- Clicking an alert navigates to the relevant inventory report entry

**Pattern Cards** (grid layout, 3 columns):
- One card per pattern
- Card border color and status badge reflect worst status across all sizes:
  - Green "Healthy" — all sizes above reorder trigger
  - Amber "Low Stock" — at least one size at or below reorder trigger
  - Red "Deficit" — at least one size has negative projected inventory
- Large sheet count (total across all sizes) in matching status color (green/amber/red)
- Inline table showing per-size breakdown: Size, Stock, On Order
  - Individual stock counts color-coded per size
  - Sorted by ascending thickness, then by width×length within same thickness
- Open order and future claims summary text

**Upcoming Planned Claims** (table below cards):
- All PlannedClaims sorted by ScheduledDate ascending
- Columns: Pattern, Size, Sheets, Scheduled Date, SO Number
- Rows for patterns with deficit situations highlighted in red

### Place Order

Left panel: form. Right panel: context.

**Form fields:**
- Pattern (dropdown from Patterns table)
- Width, Length, Thickness (dropdowns from Sizes table)
- Quantity (number input)
- PO Number (text input)
- Order Date (date picker)
- ETA Date (date picker)
- Note (optional textarea)
- "Place Order" button (green)

Thickness dropdown shows material type hint ("Compact laminate" or "Plastic laminate").

**Context panel** (updates when pattern selection changes):
- Current Inventory table for selected pattern: Size, In Stock, On Order — sorted by ascending thickness
- Open Orders table for selected pattern: PO, Size, Ordered, Received, Outstanding, ETA — sorted by ascending thickness

### Receive Shipment

**Form fields:**
- Select Open Order (dropdown showing all unfilled orders: "PO — Pattern Size (N outstanding)")
- Auto-populated order details panel (pattern, size, type, ordered, received so far, outstanding, order date, ETA)
- Quantity Received (number input, max = outstanding balance)
- Date Received (date picker)
- Note (optional textarea)
- Preview message: "This will complete PO Xxx" or "N remaining after this receipt"
- "Record Receipt" button (blue)

**Receipt History** (below form):
- Table showing previous receipts for the selected order: Date, Qty, Note

### Pull Sheets

**Mode toggle** at top: "Pull Now" (red, default) / "Schedule Future Pull" (amber)

**Form fields:**
- Pattern (dropdown)
- Width, Length, Thickness (dropdowns)
- Quantity (number input)
- SO Number (text input)
- Date — labeled "Pull Date" in Pull Now mode, "Scheduled Date" in Schedule mode (date picker)
- Note (optional textarea)
- Inventory impact preview: "Current: N → After pull: M" with color coding
- Button: "Pull Sheets" (red) in Pull Now mode, "Schedule Pull" (amber) in Schedule mode

**Pull Now validation:** Cannot pull more than current inventory for that Pattern+Size.

**Schedule Future Pull warning:** If the planned claim would push Projected Balance negative, show an amber warning: "This claim would result in a deficit of N sheets for [Pattern] [Size]." Non-blocking — user can still save.

### Inventory Report

**Filters:** Pattern dropdown (default: All Patterns). Today's date displayed.

**Export:** CSV and PDF buttons.

**Columns:**
| Column | Description |
|--------|-------------|
| Pattern | Pattern name |
| W×L×T | Combined dimensions |
| In Stock | Current inventory count |
| Last Adj. | Date of most recent transaction affecting this position |
| On Order | Outstanding quantity across all open orders |
| Order Date | Earliest open order date |
| ETA | Earliest open order ETA |
| Committed Before Arrival | PlannedClaims with ScheduledDate before ETA |
| Projected at Arrival | In Stock − Committed Before Arrival |
| Total Committed | All PlannedClaims |
| Projected Balance | In Stock + On Order − Total Committed |
| Re-Order? | "Re-order may be needed" if Projected Balance ≤ ReorderTrigger |

**Color coding:**
- In Stock: green if healthy, amber if ≤ trigger, red if zero or negative
- Projected at Arrival: red if negative
- Projected Balance: green if healthy, amber if ≤ trigger, red if negative
- Re-Order column: amber text when active

**Sort order:** By pattern name, then ascending thickness within each pattern.

### Removal Report

**Filters:**
- Start Date and End Date (date pickers)
- "Include Inactive" toggle (off by default) — when off, only patterns+sizes with removals in the period are shown

**Export:** CSV and PDF buttons.

**Columns:**
| Column | Description |
|--------|-------------|
| Pattern | Pattern name |
| W×L×T | Combined dimensions |
| Sheets Removed | Count of ActualPulls in the date range |
| Last Removal Date | Most recent pull date in the range |

This report is submitted to Polilam for billing purposes.

### Transaction Report

**Filters:** Pattern dropdown (default: All Patterns).

**Export:** CSV and PDF buttons.

**Columns:**
| Column | Description |
|--------|-------------|
| Date | Transaction date |
| Type | Badge: Initial (green), Order (blue), Receipt (amber), Pull (red) |
| Pattern | Pattern name |
| W×L×T | Combined dimensions |
| Qty | Signed quantity (+N for additions, −N for pulls) |
| PO / SO | PO number for orders/receipts, SO number for pulls |
| Note | Optional note text |

Shows a unified chronological timeline of all transaction types. Entries are sorted by date descending (newest first).

### Settings

**Manage Patterns:**
- Table of patterns with Name and Reorder Trigger columns
- Add, edit, delete functionality
- Cannot delete a pattern that has existing transactions

**Manage Sizes:**
- Separate lists for Widths, Lengths, and Thicknesses
- Add and remove values
- Cannot remove a value that is referenced by existing transactions
- Material type label shown next to thickness values

**App Version:**
- Display current application version number

---

## Visual Design

### Framework
Bootstrap 5 with a dark theme. Card-based layout with a persistent dark sidebar navigation.

### Color System
- **Green (#28a745)** — Healthy inventory, additions, positive status
- **Amber (#ffc107)** — Low stock warnings, reorder alerts, partial fills
- **Red (#dc3545)** — Deficit situations, pulls/removals, negative projections
- **Blue (#4fc3f7)** — On-order quantities, informational highlights
- **Dark backgrounds** — #0d1520 (deepest), #1a2332 (cards), #2a3a4a (borders)

### Conventions
- Sheet counts always match their status color (green/amber/red)
- Alternating row backgrounds in tables for readability
- All inventory and size listings sorted by ascending thickness, then width×length within same thickness
- Negative values displayed in red with bold weight
- Empty/zero values shown as "—" in gray
- Transaction type badges with colored backgrounds matching their category

---

## Technical Details

### Stack
- **Backend:** ASP.NET Core 8 MVC with Razor Pages
- **Database:** SQLite via Entity Framework Core
- **Frontend:** Bootstrap 5, no SPA framework — server-rendered pages with minimal JavaScript for dynamic form updates
- **Export:** CSV generation via custom middleware; PDF generation via a lightweight library (e.g., QuestPDF or similar)

### Auto-Conversion of Planned Claims
On application startup and on each page request (via middleware), query PlannedClaims where ScheduledDate ≤ today. For each:
1. Create a new ActualPull with the same fields
2. Delete the PlannedClaim

This ensures inventory calculations are always current without requiring a separate scheduled job.

### Deployment
- Runs on a Windows server on the company LAN
- Deployed as an IIS site or a Windows Service using `dotnet publish`
- SQLite database file stored alongside the application
- No external service dependencies

### Data Integrity
- Foreign key constraints enforced at the database level
- Pattern and size dropdowns populated from the database — no freeform entry for these fields
- Deletion of patterns or sizes blocked if referenced by existing transactions
- Inventory is always computed, never stored — eliminates sync/drift issues
