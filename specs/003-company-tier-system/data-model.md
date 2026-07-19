# Data Model: Company Tier System

## Entities

### Company (Updated)

Represents a registered business customer with tier-related properties.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, Required | Unique identifier |
| Name | string | Required, max 200 | Company name |
| TaxId | string | Optional, max 20 | Tax identification number |
| WebsiteUrl | string? | Optional | Company website |
| IsVerifiedFromBdex | bool | Default false | BDEX verification status |
| CurrentYearPurchaseValue | decimal | >= 0, default 0 | YTD purchase total in THB |
| CurrentYearOrderCount | int | >= 0, default 0 | YTD order count |
| Tier | string | "Classic" / "Silver" / "Gold", default "Classic" | Current tier level |
| TierCalculatedAt | DateTime? | Optional | Last tier calculation timestamp |
| CreatedAt | DateTime | Required | Record creation time |
| UpdatedAt | DateTime | Required | Last update time |

**State Transitions:**
- Tier: Classic → Silver → Gold (promotion)
- Tier: Gold → Silver → Classic (demotion, never below Classic)
- YTD values reset to 0 on January 1st UTC

---

### CompanyTierSettings (New)

Configurable tier thresholds and benefits. Supports multiple versions via ValidFrom/ValidTo.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, Required | Unique identifier |
| TierName | string | Required, "Classic" / "Silver" / "Gold" | Tier level name |
| MinPurchaseValue | decimal | >= 0 | Minimum YTD purchase for this tier |
| MinOrderCount | int | >= 0 | Minimum YTD order count for this tier |
| DiscountPercentage | decimal | 0-100 | Discount applied to orders |
| FreeShippingMinOrder | decimal? | Optional | Minimum order for free shipping |
| CoinRewardPercentage | decimal? | Optional, 0-100 | Coin reward percentage |
| ValidFrom | DateTime | Required | Settings effective from |
| ValidTo | DateTime? | Optional | Settings effective until |
| xmin | uint | Required | PostgreSQL optimistic concurrency |

**Validation:**
- Only one active setting per TierName at any time
- Concurrent updates handled via xmin (HTTP 409 on conflict)

---

### CompanyDocument (New)

Document records attached to companies for compliance and verification.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, Required | Unique identifier |
| CompanyId | Guid | Required, FK | Reference to Company |
| DocumentType | string | Required | "TaxCert" / "BusinessLicense" / "Contract" / "Other" |
| FileName | string | Required, max 255 | Original file name |
| FileUrl | string | Required | GCS URL |
| ExpiryDate | DateTime? | Optional | Document expiration |
| CreatedAt | DateTime | Required | Upload timestamp |
| xmin | uint | Required | PostgreSQL optimistic concurrency |

---

## Relationships

```
Company (1) ──────< CompanyTierSettings (many)
Company (1) ──────< CompanyDocument (many)
```

---

## Business Rules

1. **Promotion**: Company promotes when BOTH MinPurchaseValue AND MinOrderCount thresholds are met (>=)
2. **Demotion**: At year-end, companies demote by exactly one tier level
3. **Reset**: YTD values reset to 0 on January 1st UTC
4. **Default Tier**: New companies start at "Classic"
5. **Concurrency**: TierSettings and CompanyDocument use xmin for optimistic locking

---

## Migration Requirements

- Add columns to Companies table: CurrentYearPurchaseValue, CurrentYearOrderCount, Tier, TierCalculatedAt
- Create CompanyTierSettings table
- Create CompanyDocuments table
