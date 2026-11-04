# Interface languages

Affinisaur embeds its interface catalogs in the portable executable. Selecting a language does not contact a translation service. The onboarding picker advances on selection; Settings → Display → Language saves a new preference and offers an explicit **Apply and restart** action. Restarting preserves the team and other settings. Abandoning onboarding restores the previously active language.

The initial language set is English, French, German, Spanish, Italian, Portuguese, Dutch, Polish, Czech, Slovak, Slovenian, Hungarian, Romanian, Bulgarian, Greek, Swedish, Danish, Finnish, Estonian, Latvian, Lithuanian, Irish, Norwegian Bokmål, Ukrainian, Russian, Turkish, Albanian, Catalan, Basque and Galician. This is a supported selection of European languages, not an exhaustive list of every language spoken in Europe.

## Translation status

Non-English catalogs are an initial **beta** translation pass. French has reviewed overrides for onboarding, common controls, suggestion terminology and reset confirmation. Other copy still benefits from native-speaker review. User-entered names, external release notes, URLs, keyboard identifiers, model IDs and the original legal document are deliberately preserved; legal-document navigation and explanatory interface text are translated.

Only interface strings are translated. `Model.Names` and `KeyBindings.Actions` remain stable English identifiers; views translate their display text separately. Preferences keep their existing data formats. `language.txt` contains the chosen code. Missing or invalid saved codes fall back to English.

## Maintenance

* `data/locale-en.txt` is the English source catalog; `data/locale-<code>.txt` contains readable UTF-8 JSON maps. Both build paths compress them into embedded `.txt.gz` resources. The runtime decodes only the selected catalog in memory, without extracting files or contacting a service.
* Use `Language.T("Source text")` at presentation boundaries. Do not translate user data, resource filenames, reflection names or identifiers used for calculations.
* `npm run check` verifies catalog coverage, source literals, keyboard/type-token preservation and localized UI behavior. Test language changes with isolated diagnostic profiles.
* For previews, set `AFFINISAUR_PREVIEW_LANGUAGE=fr` (or another supported code) before invoking the diagnostic executable. This setting exists only in the diagnostic build.
* Keep reviewed French corrections in `tools/localization/overrides.fr.json`. Overrides win over generated output.

## Generation and attribution

The initial catalogs were prepared locally with [Argos Translate](https://github.com/argosopentech/argos-translate) 1.11.0 and CTranslate2 4.8.5, using English-to-target packages from the [Argos package index](https://github.com/argosopentech/argospm-index). Argos and the models are development tools only; none is bundled or required by the runtime. Do not regenerate reviewed text casually.

Many packages derive from OPUS-MT, by Jörg Tiedemann and Santhosh Thottingal, *OPUS-MT — Building open translation services for the World*, EAMT 2020. Package README files contain their individual model attribution and license notices (including CC BY 4.0 for the inspected English–French package). The generator preserves shortcut tokens and line breaks, translates sentences separately, and applies reviewed overrides. The source inventory and generated catalogs should still be reviewed for context and text expansion.

`tools/localization/catalog.py` inventories literals. `translate.py` is an optional maintenance tool for an isolated Python environment with `argostranslate`; it downloads models into a temporary tooling cache. The one-time migration scripts document the original literal migration and are not part of normal builds.
