# Marmotte.Devops visual generation prompts

The current Marmotte.Devops assets are documented after the historical Marmotte.Build prompts below. Historical prompts retain the exact names and instructions used at the time.

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
- Output: [historical Marmotte.Build coding banner](references/marmotte-build-banner-v2.png), preserved as the input to the Marmotte.Devops banner.
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

## Marmotte.Devops banner: separate Build and Deploy workflows

- Date: 2026-11-28.
- Tool: built-in `imagegen` (image edit with `transparent_background: true`).
- Input: [historical Marmotte.Build artwork](references/marmotte-build-banner-v2.png).
- Output: [marmotte-devops-banner.png](marmotte-devops-banner.png).
- The generated PNG was copied into the repository without further image processing.

### Exact prompt

```text
Use case: precise-object-edit.
Asset type: wide transparent PNG banner for the Marmotte.Devops GitHub README.
Input image 1: edit target and visual identity reference, the existing Marmotte.Build coding banner.
Primary request: rename the project to Marmotte.Devops and show its two separate areas, Build and Deploy, while preserving the current mascot, laptop scene, wooden sign, and painterly illustration.
Main sign text (verbatim): "Marmotte.Devops". Spell exactly M-a-r-m-o-t-t-e-.-D-e-v-o-p-s, with this capitalization and period. Retain large rounded dimensional serif lettering, cream-white "Marmotte." and golden-orange "Devops", dark outlines and warm carved shadows. Center the full name naturally on the wooden sign.
Laptop screen: replace the old code with a tidy dark editor/dashboard. At the top show two readable file tabs, "build.cs" and "deploy.cs", plus a small "C#" badge. Below, show two clearly separated horizontal groups. The first is headed "Build" and contains "Restore → Build → Test → Pack". The second is headed "Deploy" and contains two simple destination cards labeled "SSH" and "WinRM", with small server icons. Keep typography clean and legible and the interface understated; do not invent code or APIs.
Invariants: same friendly turquoise furry marmot actively typing with both paws on the laptop, cream muzzle, large white incisors, brown nose, expressive eyes, red scarf with white Swiss cross and mountain patterns. Preserve the wooden plank sign, white mountain emblem, turquoise wave decoration, warm highlights, and rich painterly cartoon style. Keep the laptop and mascot in their existing relative positions.
Composition: wide landscape around 2:1; sign across the lower half, mascot and laptop above it. Keep every edge inside the frame with a small transparent safety margin.
Background: actual alpha transparency outside the artwork, suitable for light and dark README themes. No solid backdrop or painted checkerboard.
Avoid: the old main title "Marmotte.Build", "DevOps" capitalization, extra slogans, watermarks, extra mascots or limbs, hard hats, construction tools, cropping, or redesigning the mascot.
```

## Marmotte.Devops logo: renamed wooden sign

- Date: 2026-11-28.
- Tool: built-in `imagegen` (image edit with `transparent_background: true`).
- Input: [historical Marmotte.Build artwork](references/marmotte-build-banner-v1.png).
- Output: [marmotte-devops-logo.png](marmotte-devops-logo.png).
- The generated PNG was copied into the repository without further image processing.

### Exact prompt

```text
Use case: text-localization.
Asset type: transparent PNG logo for Marmotte.Devops documentation and reuse.
Input image 1: edit target, the original Marmotte.Build logo with the mascot leaning over a wooden sign.
Primary request: replace only the project name with Marmotte.Devops. Preserve the existing illustration and the mascot pose.
Text (verbatim): "Marmotte.Devops". Spell exactly M-a-r-m-o-t-t-e-.-D-e-v-o-p-s, with this capitalization and period. No remaining "Marmotte.Build" title.
Typography: preserve the large rounded dimensional serif lettering, cream-white "Marmotte." and golden-orange "Devops", dark outlines and carved/drop-shadow depth. Rebalance letter spacing so the full name is naturally centered on the sign.
Invariants: same smiling turquoise furry marmot with eyes closed, cream muzzle, prominent white incisors, brown nose, red scarf with white Swiss cross and mountain patterns; same paws on the wooden sign, warm sculpted planks, white mountain emblem above the name, turquoise wave decoration below, painterly cartoon rendering and warm highlights.
Composition: wide landscape close to the reference's 2:1 proportions, all mascot and sign edges fully visible with a small transparent safety margin.
Background: actual alpha transparency outside the artwork, suitable for light and dark themes. No solid backdrop or painted checkerboard.
Avoid: "DevOps" capitalization, extra text, slogans, watermarks, laptops, new props, new characters, cropping, or redesigning the mascot.
```
