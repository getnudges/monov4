import { RelayEnvironmentProvider } from "react-relay";

import RelayEnvironment from "./RelayEnvironment";
import Routes from "./routes";
import { Router } from "wouter";
import Layout from "./components/Layout";

import "./App.css";

function App() {
  return (
    <Router>
      <RelayEnvironmentProvider environment={RelayEnvironment}>
        <Layout>
          <Routes />
        </Layout>
      </RelayEnvironmentProvider>
    </Router>
  );
}

export default App;
