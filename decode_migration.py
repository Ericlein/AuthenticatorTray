import base64, urllib.parse
import auth_migration_pb2

# Paste your migration link here
url = "otpauth-migration://offline?data=CiAKCprWB1GRNL2n1YQSA0hNRxoHT3BlblZQTiABKAEwAgogCgrEOP0wPNBXjAQcEgNNQ0IaB09wZW5WUE4gASgBMAIKOwoUB4erESlo0lt6OpzJMDpRjsffLtcSB1NSRzM5RVMaFEFzY2lvK1BhcnRuZXIrUG9ydGFsIAEoATACCjYKFAXElR1FutIKRVM9XH0g%2BjWrE3n/Eg9lcmljQGxhdXJpbGEuc2UaB0Rpc2NvcmQgASgBMAIKMQoKUjw/mstW6tI1GBIUZXNqb2JlcmdAODEuMjcuNDIuMTAaB09wZW5WUE4gASgBMAIKMQoKE/wk3PF6o60I1xIPZXJpY0BsYXVyaWxhLnNlGgxBc2NlbnNpb24uZ2cgASgBMAIKOwoUi3Xs8GIHbfvxqZa1QMRZjXENPQQSFGVyaWNzam8xMTlAZ21haWwuY29tGgdTaG9waWZ5IAEoATACClQKFBFYwbt2/Q0ynqKPjV/Zds38/KXUEiFBdXRoZW50aWNhdG9yIDEvZXJpY0Blcmlja2xlaW4uc2UaE294LmhtZzkud2ViaHVzZXQubm8gASgBMAIKJAoKqNrRAZ0YlZ1lbhIIRXJpY2xlaW4aBkdpdEh1YiABKAEwAhACGAEgAA%3D%3D"

data = urllib.parse.parse_qs(urllib.parse.urlparse(url).query)["data"][0]
payload = base64.urlsafe_b64decode(data)

migration = auth_migration_pb2.MigrationPayload()
migration.ParseFromString(payload)

for otp in migration.otp_parameters:
    secret_b32 = base64.b32encode(otp.secret).decode("utf-8")
    print("Account:", otp.name)
    print("Issuer:", otp.issuer)
    print("Secret:", secret_b32)
    print("Digits:", otp.digits)
    print("Algorithm:", otp.algorithm)
    print("---")
