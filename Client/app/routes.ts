import { type RouteConfig, index, route } from "@react-router/dev/routes";

export default [
  index("routes/home.tsx"),
  route("login", "routes/login.tsx"),
  route("register", "routes/register.tsx"),
  route("confirm-email", "routes/confirm-email.tsx"),
  route("forgot-password", "routes/forgot-password.tsx"),
  route("reset-password", "routes/reset-password.tsx"),
  route("dashboard", "routes/dashboard.tsx"),
  route("admin", "routes/admin-dashboard.tsx"),
  route("map", "routes/map.tsx"),
  route("list", "routes/list.tsx"),
  route("settings", "routes/settings.tsx"),
  route("station/:brandName/:city/:street/:houseNumber", "routes/station.tsx"),
  route("admin/stations", "routes/gas-station-admin.tsx"),
  route("admin/users", "routes/user-admin.tsx"),
  route("admin/brands", "routes/brand-admin.tsx"),
  route("admin/proposals", "routes/proposals-admin.tsx"),
] satisfies RouteConfig;
