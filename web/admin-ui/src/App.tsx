import { RelayEnvironmentProvider } from "react-relay";

import RelayEnvironment from "./RelayEnvironment";
import Routes from "./routes";
import { Router } from "wouter";
import Layout from "./components/Layout";

import "./App.css";
import { AuthProvider } from "./AuthProvider";

function App() {
  return (
    <Router>
      <RelayEnvironmentProvider environment={RelayEnvironment}>
        <AuthProvider>
          <Layout>
            <Routes />
          </Layout>
        </AuthProvider>
      </RelayEnvironmentProvider>
    </Router>
  );
}

export default App;
