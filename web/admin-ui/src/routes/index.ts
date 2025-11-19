import LoadingScreen from "../LoadingScreen";
import createRouterFactory from "../Router/createRouterFactory";
import withRelay from "../Router/withRelay";
import HomeRoute from "./Home/route";
import PlansRoute from "./Plans/route";

export const routes = [HomeRoute, PlansRoute];

const router = withRelay(createRouterFactory(true), routes, LoadingScreen);

export default router;
