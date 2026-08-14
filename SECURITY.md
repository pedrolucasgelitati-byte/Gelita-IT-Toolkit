# Security and release policy

The Toolkit is a local support utility. Its administrative features can resemble malicious behavior because they install software silently, manage printers and restart the print spooler. These actions must remain user-initiated and logged.

## Release requirements

1. Build only from a reviewed, clean commit.
2. Do not include `.env`, certificates, private keys, SentinelOne tokens, or credentials in a release.
3. Validate every bundled installer with both a pinned SHA-256 hash and its vendor Authenticode signature.
4. Sign every released EXE and DLL with an organization-approved code-signing certificate and an RFC 3161 timestamp. Never use a self-signed certificate for distribution.
5. Record the commit, version, SHA-256 of the final EXE, signer subject and build date in the release notes.
6. Submit the signed hash/file to Global IT for SentinelOne review. An exclusion must be approved by Global IT and scoped to the signer or exact hash; never disable SentinelOne.

Use `Scripts\Publish-Signed.ps1 -CertificateThumbprint <thumbprint>` after installing the approved certificate in the current user's certificate store. The script deliberately accepts a thumbprint rather than a PFX password so private-key material is not stored in the repository.

## Known sensitive component

`Assets\SentinelOne` is local-only and may contain a site token. Prefer deployment through the Global IT management platform. If this feature remains necessary, rotate exposed tokens, restrict access to the release package and obtain explicit Global IT approval.
