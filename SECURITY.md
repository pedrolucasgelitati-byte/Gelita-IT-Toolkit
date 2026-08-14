# Security and release policy

- Build releases only from reviewed commits.
- Never include `.env`, certificates, private keys, SentinelOne tokens or credentials in a release.
- Validate bundled installers with pinned SHA-256 hashes and vendor signatures.
- Sign released binaries with an organization-approved code-signing certificate and RFC 3161 timestamp. Do not distribute binaries signed with a self-signed certificate.
- Record the release version, commit and final executable SHA-256.
- Submit the signed file or hash to Global IT for SentinelOne review. Never disable endpoint protection.

`Assets\SentinelOne` is local-only and may contain a site token. Deployment through the Global IT management platform is preferred.
