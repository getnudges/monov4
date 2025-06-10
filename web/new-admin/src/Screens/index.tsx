import LoadingScreen from "../LoadingScreen";
import createRouterFactory from "../Router/createRouterFactory";
import withRelay from "../Router/withRelay";
import LoginRoute from "./Login/route";
import HomeRoute from "./Home/route";
import PlanRoute from "./Plan/route";
import PlansRoute from "./Plans/route";
import DiscountCodeRoute from "./DiscountCode/route";

export const routes = [LoginRoute, HomeRoute, PlanRoute, PlansRoute, DiscountCodeRoute];

const router = withRelay(createRouterFactory(true), routes, LoadingScreen);

export default router;
