import type { Route } from "./+types/home";
import Login from "./login";

export function meta({}: Route.MetaArgs) {
  return [
    { title: "Logowanie" },
    { name: "description", content: "Strona logowania użytkownika" },
  ];
}

export default function Home() {
  return <Login />;
}
