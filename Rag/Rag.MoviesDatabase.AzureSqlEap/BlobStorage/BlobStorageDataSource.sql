CREATE EXTERNAL DATA SOURCE BlobStorageContainer
WITH (
	TYPE = BLOB_STORAGE,
	LOCATION = 'https://lennidemo.blob.core.windows.net/datasets',
	CREDENTIAL = BlobStorageCredential
)
