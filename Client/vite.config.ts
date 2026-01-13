import { reactRouter } from "@react-router/dev/vite";
import tailwindcss from "@tailwindcss/vite";
import { defineConfig } from "vite";
import tsconfigPaths from "vite-tsconfig-paths";

export default defineConfig({
  plugins: [tailwindcss(), reactRouter(), tsconfigPaths()],
  
  resolve: {
    dedupe: ["react", "react-dom", "react-router", "recharts"],
  },
  
  optimizeDeps: {
    include: ["leaflet", "react", "react-dom", "recharts"],
  },

  server: {
    port: 4000,
    host: "0.0.0.0",
    hmr: {
      clientPort: 443,
    },
    allowedHosts: ["localhost", "nginx-dev"],
    watch: {
      usePolling: true,
    },
  },
});