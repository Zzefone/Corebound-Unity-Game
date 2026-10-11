# Corebound - Windows player verification

Date: 2026-10-11 (Europe/Paris).
Unity: 6000.0.23f1
Result: PASS - 28/28 checks passed.

The actual Windows build was launched with an opt-in verification argument. Real physics frames were used for boosts, collisions and triggers. Some trigger checks teleport the player into position; this is not a complete human keyboard playthrough.

| Check | Result | Evidence |
| --- | --- | --- |
| Menu is initial scene | PASS |  |
| Menu start button exists | PASS |  |
| Start button transitions to Game | PASS |  |
| Eight cores instantiated at runtime | PASS |  |
| Sphere rests on physical deck | PASS | (0.00, 0.55, 0.00) |
| Audio clips are assigned | PASS |  |
| Exit is locked before collecting | PASS |  |
| Grounded boost applies | PASS |  |
| Boost cannot be repeated during cooldown | PASS |  |
| Impulse traverses first gap | PASS | (0.00, 0.95, 24.06) |
| Checkpoint activates through trigger | PASS |  |
| Boost recharges | PASS |  |
| Second gap boost applies | PASS |  |
| Impulse traverses second gap | PASS | (0.00, 0.54, 47.31) |
| Second checkpoint activates | PASS |  |
| Falling respawns once | PASS |  |
| Scanner physically moves | PASS |  |
| Scanner collision triggers respawn | PASS |  |
| Trigger collection recovers all eight cores | PASS |  |
| HUD updates collection count | PASS |  |
| Respawning retains collected cores | PASS |  |
| Pause freezes timer and reveals panel | PASS |  |
| Resume restores time | PASS |  |
| Exit trigger wins after collection | PASS |  |
| Victory stops player | PASS |  |
| Quit button is accessible over victory | PASS |  |
| Restart resets game state | PASS |  |
| Return to menu restores initial scene | PASS |  |

Screenshots of menu, arrival, pause and victory were rendered from the running player and visually reviewed. A hidden player uses explicit camera rendering for screenshots.

The Corebound build/import completed successfully. The verifier returned to MainMenu and used its Quit handler to terminate the Windows process successfully.

Limitations: real keyboard/mouse input and subjective audio loudness were not manually reviewed. Quit button accessibility was checked structurally; the menu Quit handler verified actual application termination. A human should review difficulty and movement feel before final hand-in.
