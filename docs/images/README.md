# Image provenance

Marmotte.Build reuses the visual identity of Marmotte.ChatUi: a turquoise marmot with a cream muzzle, large incisors, and a red scarf with white patterns.

The images are stored here so the README does not depend on externally hosted images.

## Marmotte.Build banner

[marmotte-build-banner.png](marmotte-build-banner.png) was created with the built-in imagegen tool. It shows the mascot typing on a laptop with a C# `build.cs` file and the pipeline steps **Restore → Build → Test → Pack**. It preserves the mascot's identity, wooden sign, **Marmotte.Build** lettering, mountain emblem, turquoise decoration, and transparent background.

The artwork was developed in two steps: first adapting the [Marmotte.ChatUi banner reference](references/marmotte-chatui-banner.png) to the Marmotte.Build name, then adding the coding scene. The [first Marmotte.Build banner](references/marmotte-build-banner-v1.png) is preserved as the input to that second edit.

The banner is used at the top of the project README. The exact prompt and generation details are recorded in [generation-prompts.md](generation-prompts.md).

## Unmodified source assets

These files were copied without modification from the local Marmotte.ChatUi repository:

| Local asset | Source in Marmotte.ChatUi | Use |
| --- | --- | --- |
| [marmotte-chatui-banner.png](references/marmotte-chatui-banner.png) | `docs/images/marmotte-chatui-banner.png` | Original reference for the Marmotte.Build banner. |
| [marmotte-mascot.png](marmotte-mascot.png) | `docs/images/marmotte-chatui-mascot.png` | Compact mascot retained for reuse. |
| [experimental-proof-of-concept.png](references/experimental-proof-of-concept.png) | `docs/images/references/experimental-proof-of-concept.png` | Experimental project status. |

The [Marmotte.ChatUi visual identity notes](https://github.com/rflechner/Marmotte.ChatUi/blob/main/docs/images/README.md) record the original references and generation history. According to those notes, the compact mascot was generated from the supplied references using imagegen, and the experimental illustration is an unchanged user-supplied reference originating from [CodeSemantic4Llm](https://github.com/rflechner/CodeSemantic4Llm/blob/main/docs/images/experimental-proof-of-concept.png).

The source assets are retained separately from the generated Marmotte.Build banner.
