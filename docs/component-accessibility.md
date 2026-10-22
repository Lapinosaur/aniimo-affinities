# Shared interaction and accessibility contract

## Minimum contract

Interactive controls must have a meaningful accessible name, native role, keyboard entry and activation, visible focus distinct from selection, and a visibly disabled state that blocks input. Mouse, keyboard and accessible default actions must not bypass disabled or busy state. Holds cancel on lost focus, disable, hide and capture loss. Slow actions show a spinner and description immediately, reject duplicate activation, and restore their state on failure. Read-only content must expose its information without pretending to be a button.

`InteractiveButton` shares focus invalidation, keyboard pressed state, unavailable/selected/busy accessibility state and final focus/disabled painting. Native Button remains responsible for ordinary keyboard activation. `SettingsButton` adds busy feedback; `BusyOperation` keeps blocking work off the UI thread and completion on it. Do not add a second interaction implementation in a feature window.

## Component audit

| Component / consumers | Decision and resulting behavior |
| --- | --- |
| SettingsButton: settings, tabs, dialogs, team toolbar | Shared button base; focus independent of primary/selected styles; disabled segments no longer retain active fill; Space feedback; busy state and duplicate prevention. Selection is exposed for segmented choices, not every primary action. |
| IconButton: main, matrix, radar, team filters | Shared button base; disabled icons visibly muted, selected state exposed, focus repainted when keyboard cues change. |
| SupportButton: caption and About | Keeps its brand drawing, inherits action/busy behavior and common disabled/focus rendering. |
| HoldSizeButton: locked overlay scale/panel | Focus now visible; disable/hide/focus loss cancel pending work. Space holds preserve the intentional distinction from an ordinary click. Accessible action does not bypass the hold safeguard; description explains it. |
| Caption controls | Use the shared button base while preserving docking and native window actions. Keyboard pressed feedback and disabled rendering are no longer omitted. |
| Unlock / move buttons | Native Button retains standard focus and disable behavior. Move also accepts arrows and Shift+arrows; drag is no longer the only route. |
| RoundCheckBox | Native checked role and Space activation preserved; repaint on enabled and focus-cue changes. Tutorial completion indicators are non-tab-stop, non-toggle status indicators with a separate Play action. |
| DisplayChoice | Remains a native RadioButton: exclusivity and radio navigation must not be replaced by generic buttons. Disabled previews are muted; focus-cue changes repaint. |
| KeyCapture | Explicit accessible action and current value; Enter/Space starts, Escape cancels, Delete clears. Disable/hide cancels capture; unavailable controls cannot accept keys. Error feedback remains textual as well as colored. |
| NameInput / native Editor | Native text editing, selection and clipboard preserved. Accessible name is on the actual TextBox. Parent disabling propagates to editor, border and text. Placeholder is not used as the only label. |
| TypeSelector | Decorative grouping, not an extra tab stop. Actual buttons own activation. |
| TeamTypeSelector | Preserves fast Tab navigation between names; F6 enters the row's element group, arrows traverse it, Space selects, Tab exits. Ctrl+digits remains available and is described on the name field. Disabled controls reject shortcuts. |
| TeamCard / TeamBoard | Shared button states; permanent selection exposed independently of focus. Keyboard focus previews and Enter/Space selects. Names, known types and summary are exposed. Board itself is a container. |
| DrawingPanel / main affinities / radar | Decorative panel is not a tab stop. Data panels expose current multipliers as text through an accessible description. |
| Matrix cells | Existing named StaticText cells retained: data rather than hundreds of fake focusable buttons. |
| TeamMatchupPanel | Read-only Table role and current ally/opponent damage description, including not-set entries. |
| ApplicationTips | Opens on keyboard focus as well as hover; hides on focus loss/disable. Provides a plain-text description when the control has none. Events are detached on disposal. |
| Legal document / LinkLabel | Native link keyboard behavior retained; paragraphs without links are not tab stops. |
| TransitionSurface, tutorial spotlight, legends, headers | Visual-only surfaces intentionally have no button behavior. Tutorial navigation uses shared buttons; captions/body provide the explanation. |

## Verification and limits

Regression coverage checks accessible activation, unavailable state and duplicate blocking, selection and busy state, cancellation during held input, shortcut capture cancellation, composite input propagation, names in main/settings/display, tutorial completion/resume, and existing keyboard/hold/navigation suites. Visual previews cover light/dark settings, tutorial and busy actions.

This is a component-level baseline, not an accessibility certification. Automated accessibility objects do not replace manual Narrator/NVDA testing. Graph data currently uses textual descriptions rather than a fully navigable virtual cell tree; high-contrast Windows mode and all assistive-technology combinations require further manual validation.
