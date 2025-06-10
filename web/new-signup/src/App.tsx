import { RelayEnvironmentProvider } from "react-relay";

import RelayEnvironment from "./RelayEnvironment";
import Screens from "./Screens";
import { AuthProvider } from "./AuthProvider";
import { Router } from "wouter";
import Layout from "@/components/layout";

function App() {
  return (
    <Router>
      <RelayEnvironmentProvider environment={RelayEnvironment}>
        <AuthProvider>
          <Layout>
            <Screens />
          </Layout>
        </AuthProvider>
      </RelayEnvironmentProvider>
    </Router>
  );
}

export default App;
