# Product Database

```d2
--8<-- "diagrams/db/productdb.d2"
```

## `plan` Table

**Plans** are the base entity for subscription products.  They are composed of one or more Price Tiers.

## `price_tier` Table

**Price Tiers** are the units that define the price being paid for a set of features for a given time period.  For instance, for Plan 1, there may be 3 Price Tier records.  One 

## `price_tier_features` Table

## `plan_subscription` Table

## `trial_offer` Table
