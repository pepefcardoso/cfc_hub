import type { Config } from "tailwindcss";

const config = {
  darkMode: ["class", "[data-theme=\"dark\"]"],
  content: [
    './pages/**/*.{ts,tsx}',
    './components/**/*.{ts,tsx}',
    './app/**/*.{ts,tsx}',
    './src/**/*.{ts,tsx}',
  ],
  theme: {
    extend: {
      colors: {
        border: "var(--border)",
        input: "var(--input)",
        ring: "var(--ring)",
        background: "var(--background)",
        foreground: "var(--foreground)",
        primary: {
          DEFAULT: "var(--primary)",
          foreground: "var(--primary-foreground)",
        },
        secondary: {
          DEFAULT: "var(--secondary)",
          foreground: "var(--secondary-foreground)",
        },
        destructive: {
          DEFAULT: "var(--destructive)",
          foreground: "var(--destructive-foreground)",
        },
        muted: {
          DEFAULT: "var(--muted)",
          foreground: "var(--muted-foreground)",
        },
        accent: {
          DEFAULT: "var(--accent)",
          foreground: "var(--accent-foreground)",
        },
        popover: {
          DEFAULT: "var(--popover)",
          foreground: "var(--popover-foreground)",
        },
        card: {
          DEFAULT: "var(--card)",
          foreground: "var(--card-foreground)",
        },
        cfc: {
          brand: {
            primary: "hsl(var(--cfc-brand-primary))",
            secondary: "hsl(var(--cfc-brand-secondary))",
            accent: "hsl(var(--cfc-brand-accent))",
          },
          status: {
            success: "hsl(var(--cfc-status-success))",
            warning: "hsl(var(--cfc-status-warning))",
            error: "hsl(var(--cfc-status-error))",
            info: "hsl(var(--cfc-status-info))",
          },
          surface: {
            100: "hsl(var(--cfc-surface-100))",
            200: "hsl(var(--cfc-surface-200))",
            300: "hsl(var(--cfc-surface-300))",
          },
          text: {
            primary: "hsl(var(--cfc-text-primary))",
            secondary: "hsl(var(--cfc-text-secondary))",
          }
        }
      },
      borderRadius: {
        lg: "var(--radius)",
        md: "calc(var(--radius) - 2px)",
        sm: "calc(var(--radius) - 4px)",
      },
    },
  },
  plugins: [require("tailwindcss-animate")],
} satisfies Config;

export default config;
