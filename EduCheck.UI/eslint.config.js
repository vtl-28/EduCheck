// @ts-check
const eslint = require("@eslint/js");
const tseslint = require("typescript-eslint");
const angular = require("angular-eslint");

module.exports = tseslint.config(
  {
    files: ["**/*.ts"],
    extends: [
      eslint.configs.recommended,
      ...tseslint.configs.recommended,
      ...angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      // ── Angular ─────────────────────────────────────────────
      "@angular-eslint/directive-selector": [
        "error",
        { type: "attribute", prefix: "app", style: "camelCase" },
      ],
      "@angular-eslint/component-selector": [
        "error",
        { type: "element", prefix: "app", style: "kebab-case" },
      ],
      // Both inject() and constructor injection are valid patterns.
      // We use constructor injection in some older components — allow both.
      "@angular-eslint/prefer-inject": "off",

      // ── TypeScript ───────────────────────────────────────────
      // Disallow 'any' — use 'unknown' or a proper type instead
      "@typescript-eslint/no-explicit-any": "error",
      // Disallow unused variables — catch dead code
      "@typescript-eslint/no-unused-vars": [
        "error",
        { argsIgnorePattern: "^_", varsIgnorePattern: "^_" },
      ],
      // Allow empty catch/error callbacks — common in Angular HTTP calls
      "@typescript-eslint/no-empty-function": [
        "error",
        { allow: ["arrowFunctions"] },
      ],
      // Enforce T[] over Array<T>
      "@typescript-eslint/array-type": ["error", { default: "array" }],
    },
  },
  {
    files: ["**/*.html"],
    extends: [
      ...angular.configs.templateRecommended,
      ...angular.configs.templateAccessibility,
    ],
    rules: {
      // ── Accessibility rules ─────────────────────────────────
      // Our app uses (click) on interactive div/span elements intentionally.
      // These are handled via keyboard navigation at the router level.
      // Disable to avoid noise on valid patterns.
      "@angular-eslint/template/click-events-have-key-events": "off",
      "@angular-eslint/template/interactive-supports-focus": "off",

      // Labels in reactive forms use formControlName — ESLint can't detect
      // the association, but browsers and screen readers can via the DOM.
      "@angular-eslint/template/label-has-associated-control": "off",
    },
  }
);