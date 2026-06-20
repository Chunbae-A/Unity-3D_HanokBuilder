import sys
sys.stdout.reconfigure(encoding='utf-8')

from google_auth_oauthlib.flow import InstalledAppFlow
from googleapiclient.discovery import build

SCOPES = ['https://www.googleapis.com/auth/drive']
FOLDER_ID = '1J92prWdMR6HYr7WAaeIN-gsvgldHGNh9'

print("Google Drive 인증 시작 - 브라우저 창이 열립니다...")
flow = InstalledAppFlow.from_client_secrets_file('credentials.json', SCOPES)
creds = flow.run_local_server(port=0)

with open('token.json', 'w') as f:
    f.write(creds.to_json())
print("token.json 저장 완료!")

# 연결 테스트
service = build('drive', 'v3', credentials=creds)
about = service.about().get(fields='user').execute()
email = about['user']['emailAddress']
print(f"로그인 계정: {email}")

folder = service.files().get(fileId=FOLDER_ID, fields='id,name').execute()
print(f"Drive 폴더 확인: {folder['name']} (id={folder['id']})")
print("\n인증 완료! 이제 python culture_asset_downloader.py 실행 가능합니다.")
