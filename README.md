MozJPEG REST API
=====

## API

| Name | Value |
|---|---|
|Http Method| POST |
|Path|/api/MozJPEG/|
|Request Content Type |multipart/form-data|
|Response Content Type|image/jpeg|

### Parameters
|Name|Description|Default|Required|
|---|---|---|---|
|file|Your jpeg image| |Y|
|width|Resize image width| null | N|
|height|Resize image height| null | N|
|padMod|Pad image| false | N|
|padColor|Pad color| #000 | N|
