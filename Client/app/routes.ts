import { type RouteConfig, index, route } from "@react-router/dev/routes";

export default [
  index("routes/home.tsx"),
  route("login", "routes/login.tsx"),
  route("dashboard", "routes/dashboard.tsx"),
  route("admin-dashboard", "routes/admin-dashboard.tsx"),
  route("map", "routes/map.tsx"),
  route("list", "routes/list.tsx")
] satisfies RouteConfig;