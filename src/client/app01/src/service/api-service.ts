import type { ApiVersionResponse } from "./contracts/api-version-response";

export class ApiService {
    private apiUrl: string = '';
    private appToken: string = '';
    private usrToken: string = '';

    constructor(apiUrl: string, appToken: string) {
      this.setApiUrl(apiUrl);
      this.setAppToken(appToken);
    }   
    
    private setApiUrl(url: string) {
      this.apiUrl = url;
    }

    private setAppToken(appToken: string) {
      this.appToken = appToken;
    }

    public setUsrToken(usrToken: string) {
      this.usrToken = usrToken;
    }

    public getUsrToken(): string {
      return this.usrToken;
    }

    public getHeaders(): Record<string, string> {
      return {
        'Content-Type': 'application/json',
        'X-TOKEN': this.appToken,
        'Authorization': `Bearer ${this.usrToken}`,
      };
    }

    public getApiUrl(): string {
      return this.apiUrl;
    }

    public async getApiVersion(): Promise<ApiVersionResponse> {
      const response = await fetch(`${this.apiUrl}/api/version`, {
        method: 'GET',
        headers: this.getHeaders()
      });   

      if (!response.ok) {
        throw new Error(`Error fetching API version: ${response.statusText}`);
      }

      const result = await response.json();
      return result;
    }
}