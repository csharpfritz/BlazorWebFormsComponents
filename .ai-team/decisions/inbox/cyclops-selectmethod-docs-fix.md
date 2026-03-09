# Decision: SelectMethod Documentation Correction

**Author:** Cyclops (Migration Specialist)
**Date:** 2026-03-09
**Status:** Implemented

## Summary

Corrected migration documentation that incorrectly stated `SelectMethod` requires manual conversion. **`SelectMethod` IS natively supported in BWFC.**

## The Truth

### SelectMethod IS Supported

- `SelectMethod` is a parameter on `DataBoundComponent<TItemType>` (line 15-16 of `DataBoundComponent.cs`)
- Delegate signature: `IQueryable<TItemType> SelectMethod(int maxRows, int startRowIndex, string sortByExpression, out int totalRowCount)`
- Defined in `SelectHandler.cs`: `public delegate IQueryable<TItemType> SelectHandler<TItemType>(int maxRows, int startRowIndex, string sortByExpression, out int totalRowCount);`
- BWFC automatically calls `SelectMethod` in `OnAfterRender(firstRender: true)` — see lines 104-113 of `DataBoundComponent.cs`

### What IS NOT Supported (Yet)

These data methods require manual conversion to service/handler patterns:
- `InsertMethod`
- `UpdateMethod`
- `DeleteMethod`

## Files Updated

1. **`migration-toolkit/METHODOLOGY.md`**
   - Updated "What Layer 1 Does NOT Do" section to clarify SelectMethod signatures need adapting (not manual conversion)
   - Added InsertMethod/UpdateMethod/DeleteMethod as the ones that DO need manual conversion
   - Updated Layer 2 table to show SelectMethod is preserved with signature adaptation

2. **`migration-toolkit/skills/migration-standards/SKILL.md`**
   - Replaced incorrect "SelectMethod Migration (The Real Issue)" section with accurate "SelectMethod — BWFC Native Support" documentation
   - Added the correct `SelectHandler<T>` delegate signature
   - Added code example showing SelectMethod being preserved with adapted signature
   - Listed InsertMethod/UpdateMethod/DeleteMethod as the ones that need manual conversion

3. **`migration-toolkit/skills/bwfc-migration/SKILL.md`**
   - Updated Layer 2 structural transforms list
   - Updated all data binding tables to show SelectMethod is supported natively
   - Updated GridView, ListView, and FormView examples to show two options: keep SelectMethod (recommended) or use Items/DataItem binding
   - Added code examples showing the `SelectHandler<T>` signature adaptation

## Migration Guidance Summary

### Recommended Pattern (Keep SelectMethod)

```razor
<GridView SelectMethod="GetProducts" TItem="Product">
    <Columns>
        <BoundField DataField="Name" HeaderText="Name" />
    </Columns>
</GridView>

@code {
    // Adapt existing method to SelectHandler<T> signature (add 4 parameters)
    public IQueryable<Product> GetProducts(int maxRows, int startRowIndex, 
        string sortByExpression, out int totalRowCount)
    {
        totalRowCount = db.Products.Count();
        return db.Products.AsQueryable();
    }
}
```

### Alternative Pattern (Explicit Items Binding)

```razor
<GridView Items="@products" TItem="Product">
    ...
</GridView>

@code {
    private List<Product> products = new();
    
    protected override async Task OnInitializedAsync()
    {
        products = await productService.GetProductsAsync();
    }
}
```

## Rationale

The previous documentation was misleading developers into thinking they needed to completely rewrite their data loading approach when migrating. In reality, BWFC preserves the `SelectMethod` attribute and developers only need to:

1. Keep the `SelectMethod="MethodName"` attribute in markup
2. Adapt the method signature to add the 4 parameters required by `SelectHandler<T>`

This is much simpler than the previously documented approach of converting to `Items` binding with lifecycle data loading.
