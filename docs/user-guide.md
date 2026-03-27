# Polilam Inventory - User Guide (v1.1)

## Overview

Polilam Inventory tracks sheet materials (compact laminate and plastic laminate) through their full lifecycle: ordering, receiving, pulling for jobs, and reporting for billing. The sidebar on the left organizes everything into four sections: Overview, Transactions, Reports, and Admin.

---

## Dashboard

The Dashboard is your home page. It shows:

- **Pattern Cards** -- one card per pattern (e.g., Black 454, Espresso). Each card shows every size you have activity for, with current stock and on-order quantities. Cards are color-coded:
  - **Green** = healthy stock levels
  - **Amber** = low stock (at or below the reorder trigger set in Settings)
  - **Red** = deficit (negative inventory)
  - Note: sizes that only contain **drop** material (leftover pieces) will not trigger reorder warnings, since drop is not something you would reorder.

- **Alerts** -- appear at the top when any pattern/size combination has low stock or a deficit. Click an alert to jump to the relevant report.

- **Upcoming Planned Pulls** -- a table of all scheduled future pulls, showing pattern, size, quantity, scheduled date, SO#, and whether the pull would cause a deficit.

---

## Transactions

### Place Order

Use this when you place a purchase order with Polilam.

1. Select a **Pattern** from the dropdown
2. Select **Width**, **Length**, and **Thickness** (width and length default to 60" x 144")
3. Enter the **Quantity** ordered
4. Enter the **Order Date** and **ETA Date**
5. Enter the **PO Number** (your purchase order reference)
6. Optionally add a **Note**
7. Review the **Cost Per Sq Ft** field -- it's auto-filled from the pricing table based on the pattern's category, thickness, and order quantity. Override it if needed (e.g., when combining shipments for a better volume tier).
8. Click **Place Order**

The right panel shows current inventory and open orders for the selected pattern, plus the estimated cost per sheet and $/sqft.

**Overriding the price:** If you're placing two orders that will ship together (e.g., 40 + 30 sheets qualifying for the 50-99 tier), you can change the $/sqft field to the combined-volume price. The system will use your override instead of the auto-calculated tier.

### Orders

A list of all orders, sorted by ETA (most recent first). Shows Cost/Sheet and $/SqFt for each order. From here you can:

- **Edit** -- change the PO number, quantity, ETA, cost per sq ft, or note on any order. You cannot reduce the quantity below what's already been received.
- **Cancel** -- delete an order that has no receipts. If an order has partial receipts, use **Close Out** instead, which sets the ordered quantity to match what was received (effectively closing it).
- **Status badges** -- "Open" (blue) means sheets are still outstanding; "Filled" (green) means all ordered sheets have been received.

### Receive Shipment

Use this when a Polilam delivery arrives.

1. Select an **Open Order** from the dropdown -- this auto-fills the order details (pattern, size, PO#, quantities, cost per sheet and $/sqft)
2. Enter the **Quantity Received** in this shipment (must not exceed what's outstanding)
3. Set the **Date Received**
4. Optionally add a **Note**
5. Click **Receive Shipment**

The right panel shows the order's cost per sheet and $/sqft, plus receipt history so you can see what's already been received.

Partial receipts are supported -- if you ordered 10 sheets and receive 6 now, the order stays open with 4 outstanding. When the remaining 4 arrive, receive them against the same order.

Each receipt inherits the cost per sheet from its order, which feeds into the weighted average cost calculation.

### Pull Sheets

Use this when sheets are pulled from inventory for a job. There are two modes:

**Pull Now** (red button, default):
- Records an immediate pull from inventory
- The system checks that you have enough stock -- if not, it blocks the pull
- Use this when sheets are physically being taken

**Schedule Future Pull** (amber button):
- Schedules a pull for a future date
- Does NOT deduct from current inventory -- it creates a planned pull
- The system warns you if the scheduled pull would cause a deficit, but allows it
- When the scheduled date arrives, the system automatically converts it to an actual pull

For either mode:
1. Select **Pattern**, **Width**, **Length**, **Thickness**
2. Enter **Quantity**
3. Enter the **SO Number** (sales order / job reference)
4. Set the **Date** (pull date or scheduled date)
5. Optionally check **This is drop** if pulling leftover material (see Drop section below)
6. Optionally add a **Note**
7. Click **Pull Sheets** or **Schedule Pull**

The right panel shows the inventory impact -- current stock, what it will be after the pull, and the current WAC (cost per sheet and $/sqft) being attributed to this pull.

### Pulls

A list of all pulls (completed and scheduled). Shows Cost/Sheet and $/SqFt for each row (based on current WAC). From here you can:

- **Edit** -- change the quantity, scheduled date, SO#, or note on a scheduled ("Will Pull") entry
- **Cancel** -- delete a scheduled pull that's no longer needed
- Completed pulls ("Pulled") cannot be edited or canceled from this screen

### Adjust Inventory

Use this for initial stock setup, manual corrections, or adding drop material.

1. Select **Pattern**, **Width**, **Length**, **Thickness**
2. Enter **Quantity** -- positive to add stock, negative to remove/correct
3. Check **This is drop** if the material is leftover from a previous job (see Drop section below)
4. If not drop, enter the **Price Per Sq Ft** for these sheets (see Pricing section below)
5. Set the **Date**
6. Optionally add a **Note** explaining the adjustment
7. Click **Save Adjustment**

**Price Per Sq Ft behavior:**
- When there's no prior cost data for this pattern+size, the field is **required** -- you must enter the $/sqft so the system knows what these sheets are worth.
- When there is prior cost data, the field **defaults to the current weighted average cost** (shown as $/sqft). You can accept the default or override it.
- When "This is drop" is checked, the field is hidden (drop = $0).

The right panel shows current inventory, the current WAC (both per sheet and per sqft), and the projected count after your adjustment.

Common uses:
- **Initial setup** -- when first starting the app, use positive quantities to enter your current stock levels. Enter the $/sqft from the pricing table for the appropriate volume tier.
- **Adding drop material** -- check "This is drop" when adding leftover pieces back into inventory
- **Physical count corrections** -- if a physical count doesn't match the system, adjust up or down. The default $/sqft will match current WAC.
- **Damage/waste** -- use a negative quantity to remove damaged sheets

---

## Pricing and Inventory Valuation

### How pricing works

Polilam prices materials by **price per square foot**, which varies based on three factors:

1. **Pattern category** -- each pattern is either "Solid" or "Woodgrain" (set in Settings). Woodgrain patterns cost slightly more.
2. **Thickness** -- thicker sheets cost more per square foot.
3. **Volume tier** -- larger orders get better pricing:
   - 1-49 sheets per design (highest price)
   - 50-99 sheets per design (mid price)
   - 100+ sheets per design (lowest price)

The full pricing table is managed in **Settings > Sheet Pricing** and is pre-loaded with current Polilam rates.

### Cost per sheet calculation

When an order is placed, the system calculates cost per sheet automatically:

```
Cost Per Sheet = (Width × Length ÷ 144) × Price Per Sq Ft
```

For example, a 60"×144" sheet at $15.20/sqft:
- Square footage = 60 × 144 ÷ 144 = 60 sqft
- Cost per sheet = 60 × $15.20 = $912.00

### Weighted Average Cost (WAC)

The system uses **perpetual weighted average costing** to track the value of inventory. The formula is:

```
New WAC = (Sheets On Hand × Current WAC + Incoming Qty × Incoming Cost) ÷ (Sheets On Hand + Incoming Qty)
```

**How WAC updates:**
- When a shipment is received, the receipt's cost (from its order) is blended with existing inventory
- When inventory is adjusted with a cost, that cost is blended in
- Pulls reduce quantity on hand but do **not** change the WAC
- Drop material ($0) does not affect the WAC

**When stock is fully depleted:** If you use up all sheets of a pattern+size and then receive new stock at a different price, the WAC resets to the new incoming cost. This ensures old pricing doesn't contaminate the valuation of new inventory.

**Example:**
1. Start with 20 sheets at $600/sheet (WAC = $600)
2. Pull 10 sheets (on-hand = 10, WAC stays $600)
3. Receive 10 sheets at $1,200/sheet
4. New WAC = (10 × $600 + 10 × $1,200) ÷ 20 = **$900/sheet**

Throughout the app, costs are shown both as **cost per sheet** and **$/sqft** for easy reference.

The Inventory Report uses WAC to calculate stock values.

---

## Drop Material

"Drop" is leftover material from a job that's large enough to be worth saving. For example, if you pull a 60"×144" sheet for a job and the leftover 60"×72" piece is still usable, that leftover is drop.

**How drop works:**
- When adding drop back into inventory, check **"This is drop"** on the Adjust Inventory form
- When pulling drop material for a job, check **"This is drop"** on the Pull Sheets form
- Drop material has **$0 value** in reports because the original full sheet was already paid for
- Drop pulls are **excluded from the Removal Report** since Polilam was already paid for the full sheet
- Drop-only inventory **does not trigger reorder warnings** -- there's nothing to reorder

**Important:** A size like 60"×72" is not automatically considered drop. The same size could be drop (leftover) or purchased (if you buy that size directly). The "drop" checkbox on each transaction is what determines it.

---

## Reports

### Inventory Report

A snapshot of current inventory across all patterns and sizes. Shows:

- **In Stock** -- current physical inventory (includes both purchased and drop)
- **On Order** -- sheets ordered but not yet received
- **Committed Before Arrival** -- scheduled pulls that will happen before orders arrive
- **Projected at Arrival** -- what stock will look like once orders arrive and committed pulls are fulfilled
- **Total Committed** -- all scheduled future pulls
- **Projected Balance** -- stock after all commitments are fulfilled
- **Re-Order?** -- shows a warning when the projected balance is at or below the reorder trigger (excludes drop-only items)
- **Cost/Sheet** -- weighted average cost per sheet (WAC)
- **Stock Value** -- total value of purchased stock on hand (purchased quantity × WAC; drop = $0)
- **On Order Value** -- total value of sheets on order (each order's outstanding quantity × its cost per sheet)
- **Grand Total** -- sum of all stock and on-order values

Filter by pattern using the dropdown. Export to **CSV** or **PDF**.

### Removal Report

Shows all sheets actually pulled (removed from inventory) within a date range. This is the report used for Polilam billing -- they charge based on usage, not delivery.

- Defaults to the last month (same day last month through today)
- Adjust the date range as needed
- Shows quantity removed per pattern
- **Drop pulls are automatically excluded** -- you already paid for the full sheet
- **"Show all patterns" checkbox** -- when checked, includes pattern/size combinations with zero removals in the date range (useful for seeing a complete list)
- Export to **CSV** or **PDF**

Note: scheduled future pulls ("Will Pull") do not appear on the Removal Report because they haven't actually been pulled yet. They appear on the Transactions Report instead.

### Transactions Report

A detailed log of every transaction in the system. Shows:

- **Type** -- Initial (inventory adjustments), Order, Receipt, Pulled, Will Pull
- **Pattern**, **Size**, **Quantity**, **Date**, **Reference** (PO# or SO#), **ETA** (for orders)

The date range defaults to January 1st of this year through the furthest scheduled pull date, so future scheduled pulls are always visible. Adjust dates or filter by pattern as needed. Export to **CSV** or **PDF**.

---

## Admin

### Settings

Manage the reference data that drives the dropdowns and pricing throughout the app:

**Patterns** -- add, rename, or delete color/pattern names (e.g., Black 454, Espresso). Each pattern has:
- **Category** -- "Solid" or "Woodgrain", which determines pricing
- **Reorder Trigger** -- the stock level at which the Dashboard and Inventory Report show a low-stock warning

Patterns with any transaction history cannot be deleted (they show "Has transactions").

**Dimension Values** -- manage the available widths, lengths, and thicknesses that appear in dropdowns. Values with transaction history show "in use" and cannot be deleted.

**Sheet Pricing** -- the volume pricing table for all materials, organized by category (Solid/Woodgrain) and thickness. Each row has three price tiers ($/sqft):
- **1-49 sheets** -- small order pricing
- **50-99 sheets** -- mid-volume pricing
- **100+ sheets** -- high-volume pricing

The pricing table is pre-loaded with current Polilam rates. You can edit prices, add new rows for new thicknesses, or delete rows that are no longer needed.

**Version** -- shown at the bottom of the Settings page. Use the build hash to verify that the deployed version matches what you expect.

---

## Tips

- **Width and length default to 60" x 144"** on most forms since that's the most common sheet size. You can change them if needed.
- **The Dashboard is your daily starting point** -- check it for alerts and upcoming pulls.
- **Use the Transactions report to audit** -- if numbers look wrong, the transaction log shows every entry that affected inventory.
- **Mark drop correctly** -- always check "This is drop" when adding leftover material or pulling drop pieces. This keeps your removal report accurate for billing and your dollar values correct.
- **Cost per sheet is auto-calculated on orders** -- the system looks up the right price tier based on your order quantity. Override the $/sqft field if you negotiated a different price or are combining shipments.
- **Initial inventory needs a price** -- when adding starting inventory via Adjust Inventory, you'll be asked for the $/sqft. Use the appropriate tier from the pricing table.
- **WAC resets when stock is depleted** -- if you use up all sheets and reorder at a new price, the WAC reflects the new cost, not a blend with old history.
- **To start fresh** (e.g., new year or after testing), ask your IT team to reset the database. Your patterns, dimension values, and pricing table will be recreated automatically; only transaction history is cleared.
