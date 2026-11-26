# Banner generation prompts

## Version 1: Marmotte.Build title

- Date: 2026-11-26.
- Tool: built-in `imagegen` (image edit with `transparent_background: true`).
- Input: [Marmotte.ChatUi banner](references/marmotte-chatui-banner.png), copied unchanged from `Marmotte.ChatUi/docs/images/marmotte-chatui-banner.png`.
- Output: [original Marmotte.Build banner](references/marmotte-build-banner-v1.png), preserved as the input to version 2.
- The generated PNG was copied into the repository without further image processing.

### Exact prompt

```text
Use case: text-localization.
Asset type: a wide transparent PNG banner for the Marmotte.Build GitHub README.
Input image 1: edit target and visual identity reference, the existing Marmotte.ChatUi banner.
Primary request: create the matching Marmotte.Build banner by replacing only the product name on the wooden sign. Preserve the illustrated style, composition, and mascot identity.
Text (verbatim): "Marmotte.Build". Spell exactly M-a-r-m-o-t-t-e-.-B-u-i-l-d, with this capitalization and period. There must be no remaining "ChatUi" text.
Typography: retain the reference's large rounded dimensional serif lettering, cream-white "Marmotte." and golden-orange "Build", with dark outlines and carved/drop-shadow depth. Rebalance the text spacing so the shorter name feels naturally centered on the sign.
Invariants: same friendly turquoise furry marmot behind the sign on the upper left, cream muzzle, prominent white incisors, brown nose, expressive eyes, red scarf with white Swiss cross and mountain patterns; same warm sculpted wooden plank sign, white mountain emblem above the name, turquoise wave decoration below. Preserve the rich painterly cartoon rendering and warm highlights.
Composition: wide landscape banner close to the reference's 2:1 proportions, all mascot and sign edges fully visible, with a small transparent safety margin around the silhouette.
Background: actual alpha transparency outside the artwork, suitable for both light and dark GitHub themes. No solid background and no checkerboard painted into the image.
Avoid: additional text, slogans, watermarks, extra props, hard hats, tools, new characters, cropping, or redesigning the mascot.
```

## Version 2: C# build pipeline coding scene

- Date: 2026-11-26.
- Tool: built-in `imagegen` (image edit with `transparent_background: true`).
- Input: [version 1 banner](references/marmotte-build-banner-v1.png).
- Output: [current Marmotte.Build README banner](marmotte-build-banner.png).
- The generated PNG was copied into the repository without further image processing.

### Exact prompt

```text
Use case: precise-object-edit.
Asset type: wide transparent PNG banner for the Marmotte.Build GitHub README.
Input image 1: edit target, the existing Marmotte.Build banner.
Primary request: change the scene so the same marmot is actively coding a C# build pipeline at a computer, while preserving the banner's identity and prominent wooden title sign.
Scene and action: keep the marmot in the upper-left area, now looking with a delighted, focused expression toward a laptop on a small wooden work surface behind the top edge of the sign. Both paws are naturally placed on the laptop keyboard in a clear typing pose. Arrange the open laptop to the upper right of the marmot, with a believable three-quarter view that lets the viewer see both the typing paws and the screen. The screen should be large enough to read and should occupy the previously empty upper-right area. Do not obscure the mascot's face or the main title.
Screen content: a clean dark code editor, with a small "build.cs" tab and a recognizable "C#" label. Show these exact short C# lines in legible syntax-colored monospace type:
using Marmotte.Build;
Dotnet.Restore();
Dotnet.Build();
Dotnet.Test();
Dotnet.Pack();
Below the code, show a compact successful build pipeline with four connected steps labeled "Restore", "Build", "Test", and "Pack", with small green checkmarks. Keep this interface simple, tidy, and part of the laptop display.
Main sign text (verbatim): "Marmotte.Build". Retain the large dimensional rounded serif lettering, cream-white "Marmotte." and golden-orange "Build", dark outlines and warm carved shadows.
Invariants: preserve the same turquoise furry marmot, cream muzzle, large white incisors, brown nose, friendly expressive eyes, and red scarf with white Swiss cross and mountain patterns. Preserve the warm sculpted wooden sign, white mountain emblem, turquoise wave decoration, and rich painterly cartoon style of the reference. The computer should be illustrated in the same style, not pasted photographic artwork.
Composition: a balanced wide landscape banner around 2:1, sign across the lower half, coding action above it. Keep all artwork inside the frame with a small transparent safety margin; no cropped fur, scarf, laptop, or sign.
Background: actual alpha transparency outside the artwork, suitable for light and dark README themes; no solid backdrop and no painted checkerboard.
Avoid: additional mascots, extra limbs or fingers, hard hats, construction tools, watermarks, slogans, random illegible code, changing the project name, or hiding the typing action behind the sign.
```


