import { type RouteConfig, index, route } from "@react-router/dev/routes";

export default [
  index("routes/home.tsx"),
  route("login", "routes/login.tsx"),
  route("register", "routes/register.tsx"),
  route("confirm-email", "routes/confirm-email.tsx"),
  route("forgot-password", "routes/forgot-password.tsx"),
  route("reset-password", "routes/reset-password.tsx"),
  route("dashboard", "routes/dashboard.tsx"),
  route("admin-dashboard", "routes/admin-dashboard.tsx"),
  route("map", "routes/map.tsx"),
  route("list", "routes/list.tsx"),
  route("settings", "routes/settings.tsx"),
  route("proposals", "routes/proposals.tsx"),
  route("station/:brandName/:city/:street/:houseNumber", "routes/station.tsx"),
  route("gas_station_admin", "routes/gas-station-admin.tsx"),
  route("admin_admin", "routes/admin-admin.tsx"),
  route("user_admin", "routes/user-admin.tsx"),
  route("brand_admin", "routes/brand-admin.tsx"),
  route("proposals_admin", "routes/proposals-admin.tsx"),
  route("points", "routes/points.tsx"),
] satisfies RouteConfig;