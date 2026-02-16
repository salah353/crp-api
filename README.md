# CRP API (Crypto Release Platform)

The CRP API is a high-performance .NET 8/10 API that acts as a relay for cross-chain transactions. It handles transaction preparation, broadcasting, and status tracking for BSC, Solana, and Sui.

## Features

- **Relay-Based Architecture**: Separation of concerns between payload preparation and local signing.
- **Idempotency Engine**: Built-in support for client-side idempotency keys to prevent double-spending.
- **Token Management**: Configurable allowed tokens per network (Native, USDT, USDC supported by default).
- **Unified Endpoints**: Single API interface for multiple blockchains.

## API Endpoints

### `POST /api/transfer/prepare`
Prepares a transaction for a specific chain.
- **Body**: `Chain`, `Token`, `From`, `To`, `Amount`, `ClientIdempotencyKey`
- **Returns**: `IntentId`, `ToSignPayload`, `Metadata`

### `POST /api/transfer/broadcast`
Broadcasts a signed transaction to the network.
- **Body**: `Chain`, `IntentId`, `SignedPayload`
- **Returns**: `IntentId`, `BroadcastOutcome`, `TxHash`

### `POST /api/transfer/status`
Checks the finality of a broadcasted transaction.
- **Body**: `Chain`, `IntentId`
- **Returns**: `Status` (Pending, Finalized, Failed), `TxHash`, `RetryAction`

### `POST /api/wallets/balance`
Checks the balance of a specific address.
- **Body**: `Chain`, `Address`, `TokenAddress` (optional)
- **Returns**: `Chain`, `Address`, `TokenAddress`, `Balance` (raw units)

## Configuration

Settings are managed in `appsettings.json`. You must configure your RPC nodes and allowed tokens here:

```json
"BlockchainConfig": {
  "Bsc": {
    "RpcUrl": "...",
    "Tokens": [
      { "Symbol": "USDT", "Address": "0x55d...", "Decimals": 18 }
    ]
  }
}
```

## Security (Production)

1.  **API Key**: Requests require `X-API-Key` and `Accept: application/json` headers.
2.  **Environment Variables**: In production, override `ApiSecurity:ApiKey` and `BlockchainConfig` using environment variables or a Secret Manager.
3.  **HTTPS**: Always serve the API over SSL/TLS.
4.  **CORS**: Configure CORS policies in `Program.cs` if accessing from a browser.

## Minimal Production Status

- ✅ **Encapsulated Service Logic**: Code is modular and chain-specific logic is isolated.
- ✅ **Idempotency**: All transfers are protected against duplication.
- ✅ **Dynamic Configuration**: No hardcoded RPCs or Token addresses.
- ⚠️ **Persistence**: Currently uses a `ConcurrentDictionary` (In-Memory). For a horizontal production scale, implement a Persistent Repository (SQL/Redis).

## Getting Started

```bash
dotnet run
```
Default docs: `http://localhost:5073/swagger`

## License

MIT
