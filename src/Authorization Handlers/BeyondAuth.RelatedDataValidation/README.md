# Related Data Authorization Handler

This is meant to solve problems around protecting indirect / derived data.

## Prevent accidentally delivering a derived file to the wrong client
Let's say we have several systems involved in the ingesting, processing and delivery of work based on a file (or resource) provided to you by Client A, and part of this involves some steps in the workflow that are performed by separate systems which don't necessarily directly pass data among themselves.

### Example scenario
- File1 is submitted by the client BankOne to System A
- System A computes a SHA-256 hash of the file's contents and sends the hash and client name to the Related Data Authorization component
- Alice goes into System A and downloads the file and uploads it to internal processing System B
- System B generates derivative file File2
- System B computes SHA-256 hashed of the both the input and output files at any step that generates a file, and sends the new file's hash and any potentially useful additional information to the Related Data Authorization component, with the input file's hash in the 'rel' property
- Susan downloads File2 and tries to upload it in System A and deliver it to client FintechX
- System A computes a SHA-256 hash of the uploaded file and submits it to the Related Data Authorization component along with the client's name
- Validation rule fails and accidental delivery is prevented


## Register services
services.AddSingleton<IAuthorizationHandler, RelatedDataAuthorizationHandler>();
services.AddSingleton<IRelatedDataAuthorizationService, RelatedDataAuthorizationService>();

## Usage



## Todo
What should happen if ClientB uploads the same file as ClientA completely independent of each other
