 # CRP API — Chain Relay Protocol (Sui)

CRP API is a .NET API relay that prepares, broadcasts, and tracks **Sui transactions**.
Signing happens locally on the client via CRP SDK.

## Endpoints
- POST `/api/transfer/prepare`
- POST `/api/transfer/broadcast`
- POST `/api/transfer/status`
- POST `/api/wallets/balance`

## Run locally
```bash
dotnet run
````

Swagger:
[http://localhost:5073/swagger](http://localhost:5073/swagger)

## Security

* Requests require `X-API-Key`
* Use HTTPS in production

## License

MIT

 
