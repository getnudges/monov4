import Screens from "./Screens";
import { AuthProvider } from "./AuthProvider";
import { Router } from "wouter";
import Layout from "@/components/layout";

import "./App.css";
import { SnackbarProvider } from "./components/Snackbar";
import RelayEnvironmentProviderWrapper from "./RelayEnvironmentProviderWrapper";

function App() {
  return (
    <Router>
      <SnackbarProvider>
        <RelayEnvironmentProviderWrapper>
          <AuthProvider>
            <Layout>
              <Screens />
            </Layout>
          </AuthProvider>
        </RelayEnvironmentProviderWrapper>
      </SnackbarProvider>
    </Router>
  );
}

export default App;
