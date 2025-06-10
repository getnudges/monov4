import LoadingScreen from "../LoadingScreen";
import createRouterFactory from "../Router/createRouterFactory";
import withRelay from "../Router/withRelay";
import HomeRoute from "./Home/route";
import SignupRoute from "./SignUp/route";
import PlansRoute from "./Plans/route";
import PaidRoute from "./Paid/route";
import SubscribeRoute from "./Subscribe/route";
import DashboardRoute from "./Dashboard/route";
import PortalRoute from "./Portal/route";

export const routes = [
  HomeRoute,
  SignupRoute,
  PlansRoute,
  PaidRoute,
  SubscribeRoute,
  DashboardRoute,
  PortalRoute,
];

const router = withRelay(createRouterFactory(true), routes, LoadingScreen);

export default router;
