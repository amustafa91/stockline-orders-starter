# Stockline Orders · Lesson 2 completed checkpoint

Module 1: Clean Code Foundations. Lesson 2: Names that reveal business intent.

This branch follows `lesson-01-complete`. It keeps the payment adapter and changes a fixed catalog fixture from USD 12 per unit to **199 cents per unit**. It demonstrates explicit units, one cents-to-dollars conversion, and decimal arithmetic. It does not add live catalog integration or currency conversion.

## Build along

```text
git clone https://github.com/amustafa91/stockline-orders-starter.git
cd stockline-orders-starter
git switch lesson-01-complete
code .
```

Use your own completed Lesson 1 solution instead if you prefer. Implement the lesson before comparing with `lesson-02-complete`. The `main` branch remains the original Lesson 1 starter.

## Run with HTTPS

Install .NET 10 SDK, Git, VS Code with C# support, and Postman. Trust the ASP.NET Core development certificate once on each computer:

```text
dotnet dev-certs https --trust
dotnet run --project ./src/Stockline.Api/Stockline.csproj
```

Accept any OS certificate prompt yourself. The Stockline launch profile binds to **https://localhost:7375**. Import `postman/Stockline Orders.postman_collection.json`; its collection is named **Stockline Orders - Lesson 2**, and its `baseUrl` matches. Keep Postman's SSL certificate verification enabled. If your Postman installation needs the local certificate added to its CA trust, follow its certificate settings rather than disabling verification. This certificate is only for local development.

### If Postman rejects the HTTPS certificate

Trusting the development certificate in Windows does not establish that every API client will accept it. If Postman reports `Unable to verify the first certificate` or a self-signed certificate error after the Windows trust step, keep SSL certificate verification enabled and add the public development certificate to Postman's CA certificates.

From PowerShell, export the public certificate to your temporary folder:

~~~powershell
dotnet dev-certs https --export-path "$env:TEMP/stockline-localhost-public.pem" --format PEM
~~~

This command exports the public certificate only. In Postman, open **Settings → Certificates**, enable **CA Certificates**, and select `%TEMP%\stockline-localhost-public.pem` using the file picker. Use the **CA Certificates** section; this application does not require a client certificate or a private key. Repeat Create Order at `https://localhost:7375/orders` and confirm that the SSL error is gone.

Do this on each development computer if its Postman client needs it. Export that computer's own development certificate; do not download someone else's certificate or commit certificate files to this repository.

References: [Microsoft's certificate export options](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-dev-certs) and [Postman's CA certificate instructions](https://learning.postman.com/docs/use/send-requests/authorization/certificates/).

## Follow the change in OrderStore.cs

1. Confirm Lesson 1 still returns HTTP 201, Confirmed, total 24 for two units.
2. New requirement: the exercise's catalog fixture gives the integer 199, in cents.
3. The deliberately wrong first attempt uses `var price = 199` and `price * quantity`. Two units incorrectly produce 398 dollars.
4. Use Rename Symbol to change `price` to `unitPriceCents`. Renaming exposes the missing conversion but leaves behavior unchanged.
5. Derive `unitPriceUsd = unitPriceCents / 100m`. The decimal literal avoids integer division.
6. Calculate `totalUsd = unitPriceUsd * quantity` and pass it into the order constructor.
7. Keep the public JSON fields `total` and `currency` unchanged. The contract still returns a USD total.

| Check | Expected outcome |
| --- | --- |
| One unit at 199 cents | HTTP 201, Confirmed, USD 1.99 |
| Two units at 199 cents | HTTP 201, Confirmed, USD 3.98 |
| Three units at 199 cents | HTTP 201, Confirmed, USD 5.97 |
| GET the returned order ID | Same stored total and quantity |
| Quantity zero | HTTP 400 |
| Temporarily use 1200 cents, two units | USD 24.00; restore 199 afterward |

The Postman tests target the completed 199-cent fixture with the Approve scenario. They intentionally fail on the incorrect attempt. Re-import the updated collection when switching checkpoints; the old Lesson 1 collection expects USD 24. The provider outcome exercises from Lesson 1 still apply, but their totals now use this lesson's price.

## Exercise

Change the fixture to 249 cents. Predict the total for three units, send the request, and explain why renaming `price` alone would not repair the bug. Adjust the expected assertion when deliberately changing the fixture. Restore 199 cents before continuing to the next lesson.

## Folders and teaching shortcuts

- `Controllers`: HTTP input and response handling.
- `Orders`: order data, payment contract, and in-memory storage/pricing fixture.
- `Payments`: simulated provider and its adapter; no real payment is sent.
- `Properties/launchSettings.json`: local HTTPS address.
- `Program.cs`: service registrations and controller mapping.
- `postman`: executable request scenarios.

Comments explain intentional shortcuts. Restarting clears orders. The fixed catalog value keeps this lesson focused; catalog I/O, money types, persistence, and larger architectural boundaries come when later requirements justify them.
