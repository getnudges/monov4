import Routes from "./routes";
import { Router } from "wouter";
import Layout from "./components/Layout";

import "./App.css";
import { AuthProvider } from "./AuthProvider";
import RelayEnvironmentProviderWrapper from "./RelayEnvironmentProviderWrapper";
import ToasterProvider from "./components/Toaster";

function App() {
  return (
    <Router>
      <ToasterProvider>
        <RelayEnvironmentProviderWrapper>
          <AuthProvider>
            <Layout>
              <Routes />
            </Layout>
          </AuthProvider>
        </RelayEnvironmentProviderWrapper>
      </ToasterProvider>
    </Router>
  );
}

export default App;
