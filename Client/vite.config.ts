import { reactRouter } from "@react-router/dev/vite";
import tailwindcss from "@tailwindcss/vite";
import { defineConfig } from "vite";
import tsconfigPaths from "vite-tsconfig-paths";
import pluginReact from '@vitejs/plugin-react-swc';

console.log('pluginReact type:', typeof pluginReact);

export default defineConfig({
  plugins: [tailwindcss(), reactRouter(), tsconfigPaths()],
  server: {
    port: 4000,
    host: "0.0.0.0",
    watch: {
      usePolling: true,
    },
  },
});
