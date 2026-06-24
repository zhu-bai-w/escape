# Card Art Import Flow

This is the validated flow for importing generated card images into the Unity project.

## Source Inputs

- Current card art folder outside Unity: `E:\cards`
- Unity card art folder: `Assets/UniversitySimulator/Art/cards`
- Art sheet source: `卡片总览.xlsx`, sheet `美术制图表`
- Card image naming convention: `card-E###.png`
- Matching prefab naming convention: `US_E###.prefab`

## Import Rules

1. Copy every available `card-E###.png` into `Assets/UniversitySimulator/Art/cards`.
2. Configure each imported image as:
   - Texture Type: Sprite
   - Sprite Mode: Single
   - Mip Maps: disabled
   - Wrap Mode: Clamp
   - Filter Mode: Bilinear
3. For each image, find the matching prefab named `US_E###.prefab` under `Assets/UniversitySimulator/Prefabs/Cards`.
4. In that prefab, set the template `Icon` Image sprite override to the imported image sprite.
5. Also set `CardStyle.usePrefabIconOverride = true` on that prefab, otherwise `CardStyle.Refresh()` can overwrite the icon with the style default icon.

## Important Implementation Details

- Template prefab GUID: `0f0aa08f89365394b80c6b149344b23a`
- Template `Icon` Image component fileID: `114853187802903376`
- Template `CardStyle` component fileID: `114484288795279350`
- Single Sprite texture fileID: `21300000`
- The validated sprite reference format is:

```yaml
objectReference: {fileID: 21300000, guid: <card-image-meta-guid>, type: 3}
```

## Verification

After import, run Unity Play Mode verification:

- Menu: `University Simulator/Cards/Verify Available Card Art`
- Report: `Assets/UniversitySimulator/Data/card_art_verification_available.json`
- Passing condition: all available `card-E###.png` images report `matchesExpectedSprite: true`.

For visual/source diagnosis:

- Report: `Assets/UniversitySimulator/Data/card_art_diagnosis.json`
- Contact sheet: `Assets/UniversitySimulator/Data/card_art_visibility_sheet.png`

## Known Findings From 2026-06-24

- Current art sheet expects 48 images.
- Current imported image count is 46.
- Missing source images: `E008` and `E012`.
- Existing 46 images were verified at runtime: 46 passed, 0 failed.
- Transparent backgrounds are from the source PNG pixels, not Unity import settings.
- Low-visibility source images identified: `E027`, `E059`, `E062`, `E064`, `E068`, `E074`, `E084`.
