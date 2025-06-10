import { RelayEnvironmentProvider } from "react-relay";

import RelayEnvironment from "./RelayEnvironment";
import Screens from "./Screens";
import { AuthProvider } from "./AuthProvider";
import { Router } from "wouter";
import Layout from "@/components/layout";

import "./App.css";
import { SnackbarProvider } from "./components/Snackbar";

function App() {
  return (
    <Router>
      <RelayEnvironmentProvider environment={RelayEnvironment}>
        <AuthProvider>
          <SnackbarProvider>
            <Layout>
              <Screens />
            </Layout>
          </SnackbarProvider>
        </AuthProvider>
      </RelayEnvironmentProvider>
    </Router>
  );
}

export default App;
