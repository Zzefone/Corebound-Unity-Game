# Development observations

These are observations recorded while building and checking the project, rather than a completed submission post-mortem.

## Impulse and normal movement

Normal movement accelerates the Rigidbody toward an input velocity. Applying that same controller immediately after a boost would remove much of the impulse. The controller preserves boosted movement for 0.55 seconds before returning to normal acceleration. Windows player verification then exercised both gaps with a grounded impulse and no input during flight. The later gap is four units wide; the player landed on its far deck in the recorded checks.

## Hidden-window screenshots

The first Windows player verification completed all 28 gameplay checks, but screenshots taken using the presentation buffer were black because the game window was hidden. This was a capture problem: the scene transitions, physics triggers and timers were running and logged successfully. Verification now explicitly renders the active camera and Canvas to a RenderTexture, reads its pixels, and writes PNG files. It temporarily uses a camera-space HUD for capture and restores the regular screen-space overlay immediately afterward.

## Floor labels and modal cards

Visual inspection showed floor lettering behind/over the pause and victory text. The original floor lettering used legacy TextMesh font rendering. Floor labels were converted to world-space Canvas text, and the HUD hides these label canvases while a modal panel is open. Gameplay messages are also hidden during pause and cleared on victory, preventing the previous respawn message from remaining above the victory card.

## What still needs a human playthrough

The opt-in checks exercise physical flights, collisions and trigger interactions. Some checks position the player directly to isolate a trigger, rather than steering through a full run with a keyboard. The game has not been evaluated by a human for difficulty or preferred movement feel. Audio clips were synthesized, imported and assigned, but their loudness has not been reviewed by listening. Those are useful review tasks before final hand-in.
