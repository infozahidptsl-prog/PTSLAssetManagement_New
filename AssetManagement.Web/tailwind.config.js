/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class',
  content: [
    "./Components/**/*.razor",
    "./**/*.razor",
    "!./node_modules/**"
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        mono: ['"JetBrains Mono"', 'ui-monospace', 'SFMono-Regular', 'monospace'],
      },
      colors: {
        canvas: 'var(--canvas)',
        surface: 'var(--surface)',
        elevated: 'var(--elevated)',
        raised: 'var(--raised)',
        scrim: 'var(--scrim)',
        line: {
          DEFAULT: 'var(--line)',
          soft: 'var(--line-soft)',
          strong: 'var(--line-strong)',
        },
        txt: {
          hi: 'var(--txt-hi)',
          mid: 'var(--txt-mid)',
          lo: 'var(--txt-lo)',
        },
        accent: {
          soft: 'var(--accent-soft)',
          border: 'var(--accent-border)',
          on: 'var(--accent-on)',
          400: 'var(--accent-400)',
          500: 'var(--accent-500)',
          600: 'var(--accent-600)',
        },
        ok: {
          soft: 'var(--ok-soft)',
          border: 'var(--ok-border)',
          400: 'var(--ok-400)',
          500: 'var(--ok-500)',
        },
        warn: {
          soft: 'var(--warn-soft)',
          border: 'var(--warn-border)',
          400: 'var(--warn-400)',
          500: 'var(--warn-500)',
        },
        danger: {
          soft: 'var(--danger-soft)',
          border: 'var(--danger-border)',
          400: 'var(--danger-400)',
          500: 'var(--danger-500)',
        },
      },
      boxShadow: {
        panel: '-32px 0 64px -24px rgba(0, 0, 0, 0.28)',
        pop: '0 12px 32px -12px rgba(0, 0, 0, 0.32)',
      },
      letterSpacing: {
        tightest: '-0.03em',
      },
    },
  },
  plugins: [],
}
