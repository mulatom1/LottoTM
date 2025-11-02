import { useEffect, useState } from "react";
import { useAppContext } from "../../context/app-context";


function HomePage() {

  const { isLoggedIn, user, login, logout, getApiService } = useAppContext();
  const apiService = getApiService();

  const [appVersion, setAppVersion] =  useState<string>(""); 

  const getApiVersion = async () => {
    const response = await apiService?.getApiVersion();
    console.log("API Version Response:", response?.version);
    setAppVersion(response?.version ?? "");
  }

  const clickLogin = () => {
    login({ id: 1,  email: "testuser", token: "token"});
  };

  useEffect(() => {
    getApiVersion();
  }, []);

  return (
    <>
      <div className="bg-yellow-300">
        API: {import.meta.env.VITE_API_URL} 
        <br/>
        <br/>
        isUseLogged: {(isLoggedIn ? "Zalogowany" : "Niezalogowany")}
        <br/>
        <br/>
        user: {user?.email ?? ""} {user?.token ?? "" }
        <br/>
        <br/>
        version: {appVersion}
        <br/>
        <br/>
        service usr token: {apiService?.getUsrToken() ?? ""}
        <br/>
        <br/>
        <button className="bg-amber-950 text-white" onClick={clickLogin}>Login</button>
        <br/>
        <br/>
        <button className="bg-amber-950 text-white" onClick={() => logout()}>Logout</button>
      </div>
    </>
  )
}

export default HomePage
