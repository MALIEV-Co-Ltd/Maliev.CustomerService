# Feature Specification: Company Tier System

**Feature Branch**: `003-company-tier-system`
**Created**: 2026-02-25
**Status**: Draft
**Input**: Automated tier system for company customers based on yearly purchase activity with Classic/Silver/Gold tiers, configurable benefits (discounts, free shipping, coin rewards), automatic promotion/demotion, and company document management.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Automatic Tier Promotion (Priority: P1)

A company automatically gets promoted to a higher tier when their year-to-date (YTD) purchase value and order count meet the configurable thresholds.

**Why this priority**: This is the core value proposition of the tier system - rewarding loyal customers automatically without manual intervention.

**Independent Test**: Can be tested by creating test orders with various amounts and verifying tier changes occur correctly when thresholds are met.

**Acceptance Scenarios**:

1. **Given** a company at Classic tier with YTD purchases of THB 0 and 0 orders, **When** an order for THB 100,000 is paid, **Then** the company should be promoted to Silver tier.
2. **Given** a company at Silver tier with YTD purchases of THB 100,000 and 10 orders, **When** an order for THB 400,000 is paid bringing total to THB 500,000, **Then** the company should be promoted to Gold tier.
3. **Given** a company at Classic tier, **When** they have 10 orders but only THB 50,000 in purchases, **Then** the company should NOT be promoted (both thresholds must be met).
4. **Given** a company at exactly the threshold values, **When** YTD values meet or exceed thresholds, **Then** the company SHOULD be promoted (>= comparison)

---

### User Story 2 - Tier Benefit Application (Priority: P1)

Company tier benefits (discounts, free shipping threshold, coin rewards) are applied to all orders placed under that company.

**Why this priority**: This delivers the tangible value of the tier system to customers - they receive better pricing and benefits as they upgrade.

**Independent Test**: Can be tested by placing orders for companies at different tiers and verifying correct benefits are applied.

**Acceptance Scenarios**:

1. **Given** a Silver tier company with free shipping threshold of THB 5,000, **When** an order of THB 6,000 is placed, **Then** free shipping should be applied.
2. **Given** a Gold tier company with 20% discount, **When** an order of THB 10,000 is placed, **Then** a THB 2,000 discount should be applied.

---

### User Story 3 - Year-End Tier Demotion (Priority: P1)

At the end of each calendar year, companies are demoted by one tier level (not reset to Classic) while YTD values reset.

**Why this priority**: Ensures the tier system remains meaningful - companies must maintain activity to keep their status, preventing permanent tier lock-in.

**Independent Test**: Can be tested by simulating year-end conditions and verifying demotion logic.

**Acceptance Scenarios**:

1. **Given** a Gold tier company at year-end, **When** January 1st arrives, **Then** the company should be demoted to Silver tier (not Classic).
2. **Given** a Silver tier company at year-end, **When** January 1st arrives, **Then** the company should be demoted to Classic tier.
3. **Given** a Classic tier company at year-end, **When** January 1st arrives, **Then** the company should remain at Classic tier.

---

### User Story 4 - Admin Manual Tier Recalculation (Priority: P2)

Administrators can manually trigger tier recalculation for any company to correct discrepancies or handle special cases.

**Why this priority**: Provides operational flexibility to handle edge cases or correct data issues without waiting for automatic triggers.

**Independent Test**: Can be tested by manually invoking the recalculation endpoint and verifying correct tier is applied.

**Acceptance Scenarios**:

1. **Given** a company with YTD values that meet Gold thresholds, **When** admin triggers manual recalculation, **Then** company should be set to Gold tier.
2. **Given** a non-existent company ID, **When** admin triggers recalculation, **Then** an appropriate error should be returned.

---

### User Story 5 - Tier Settings Management (Priority: P2)

Administrators can configure tier thresholds and benefits through the Tier Settings API.

**Why this priority**: Allows business to adjust tier criteria without code changes, making the system flexible to business needs.

**Independent Test**: Can be tested by updating settings and verifying new thresholds are applied.

**Acceptance Scenarios**:

1. **Given** existing tier settings, **When** admin updates Silver minimum purchase to THB 150,000, **Then** the new value should be persisted and used for future calculations.
2. **Given** two admins updating the same tier setting simultaneously, **Then** one should receive a conflict error (HTTP 409).

---

### User Story 6 - Company Document Management (Priority: P3)

Companies can upload and manage supporting documents such as tax certificates and business licenses.

**Why this priority**: Supports compliance requirements and document verification for company accounts.

**Independent Test**: Can be tested by uploading, listing, and deleting documents.

**Acceptance Scenarios**:

1. **Given** a company, **When** admin uploads a tax certificate document, **Then** the document should be stored and retrievable.
2. **Given** a company with an existing document, **When** admin deletes that document, **Then** the document should be removed and no longer retrievable.

---

### User Story 7 - Tier Progress Display (Priority: P3)

The intranet displays tier progress showing current tier, YTD values, and progress toward next tier.

**Why this priority**: Provides transparency to customers about their tier status and what they need to reach the next level.

**Independent Test**: Can be tested by viewing company details and verifying progress information is accurate.

**Acceptance Scenarios**:

1. **Given** a Silver tier company with YTD purchases of THB 50,000 toward Gold (threshold THB 500,000), **When** viewing company details, **Then** progress should show 10% toward Gold.

---

### Edge Cases

- Companies at exactly threshold values are promoted (>= comparison)
- Background job failure triggers automatic retry with escalation to operations
- Tier settings updates while calculations in progress handled via optimistic concurrency
- Companies with no purchase activity in year remain at Classic tier

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST automatically calculate and update company tier when OrderPaidEvent is received.
- **FR-002**: System MUST require BOTH minimum YTD purchase value AND minimum order count to be met for promotion.
- **FR-003**: System MUST demote companies by exactly one tier level at year-end, never below Classic.
- **FR-004**: System MUST reset YTD purchase value and order count to zero on January 1st UTC.
- **FR-005**: System MUST apply tier-specific benefits (discounts, free shipping threshold, coin rewards) to all orders.
- **FR-006**: System MUST provide API endpoint for manual tier recalculation.
- **FR-007**: System MUST allow administrators to create, read, and update tier settings.
- **FR-008**: System MUST handle concurrent tier settings updates with optimistic concurrency (return HTTP 409 on conflict).
- **FR-009**: System MUST run year-end demotion job automatically via background service.
- **FR-010**: System MUST support company document upload, listing, and deletion.
- **FR-011**: System MUST return current tier information when retrieving company details.
- **FR-012**: System MUST enforce IAM permissions for all tier and document APIs.
- **FR-013**: System MUST publish tier change events for downstream services to react to.

### Key Entities

- **Company**: Represents a registered company with tier-related properties (CurrentYearPurchaseValue, CurrentYearOrderCount, Tier (string: Classic/Silver/Gold), TierCalculatedAt).
- **CompanyTierSettings**: Configurable tier thresholds and benefits (TierName, MinPurchaseValue, MinOrderCount, DiscountPercentage, FreeShippingMinOrder, CoinRewardPercentage, ValidFrom, ValidTo).
- **CompanyDocument**: Document records attached to companies (CompanyId, DocumentType, FileName, FileUrl, ExpiryDate, CreatedAt).
- **OrderPaidEvent**: External event that triggers tier recalculation (CompanyId, OrderTotal, PaidAt).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Companies automatically promoted within 5 seconds of OrderPaidEvent processing.
- **SC-002**: Tier benefit calculations are accurate for 100% of orders under tiered companies.
- **SC-003**: Year-end demotion job completes for all active companies within 1 hour of January 1st UTC.
- **SC-004**: Tier settings can be updated via API with response time under 500ms.
- **SC-005**: Concurrent tier settings updates result in clear conflict resolution (one success, one failure with 409).
- **SC-006**: Company documents can be uploaded, listed, and deleted with 99.9% success rate.

### Acceptance Criteria Checklist

- [ ] New companies start at Classic tier by default
- [ ] Tier automatically promotes when YTD values meet configurable thresholds
- [ ] Tier demotes by one level at year-end (not reset to Classic)
- [ ] Company tier benefits apply to all orders under that company
- [ ] Tier settings are configurable via API
- [ ] Year-end demotion runs automatically via background service
- [ ] Manual tier recalculation available for administrators
- [ ] Intranet displays tier progress with YTD values and next tier requirements
- [ ] Concurrent updates to tier settings handled via optimistic concurrency (HTTP 409 on conflict)

---

## Clarifications

### Session 2026-02-25

- Q: Threshold boundary logic (when YTD exactly at threshold, should promote?) → A: Promote when YTD meets OR exceeds threshold (>=)
- Q: Authentication & Authorization for Tier Management APIs → A: Use existing IAM permission model (customer.companies.read, customer.tiers.manage, customer.companies.write)
- Q: Background Job Failure Handling → A: Automatic retry with escalation (notify ops if still failing after N attempts)
- Q: Company Tier Field Storage → A: Store as string (Classic/Silver/Gold) in Company entity
- Q: Tier Change Event Logging → A: Publish tier change events for downstream services (notification, analytics)

## Assumptions

- OrderPaidEvent is already being published by the Order service and can be consumed by CustomerService.
- The background job uses a reliable scheduling mechanism (e.g., Quartz.NET) that can handle missed runs.
- Company tier benefits are read by the Order service when calculating order totals - this spec assumes the data is available but the calculation happens elsewhere.
- Documents are stored in Google Cloud Storage (GCS) with URLs provided externally. This service manages document metadata and URLs only.
