# online-course-recommendation-system
### 1. online-course-recommendation-system

	### Đồng bộ ip của mobile và .net core (điện thoại cắm usb qua laptop)

		adb reverse tcp:5128 tcp:5128
        Kết quả mong đợi: 5128

    #### Thay đổi Properties/launchSettings.jon và mobile:

		http://127.0.0.1:5128

	### Database first
	    
		dotnet ef dbcontext scaffold "Server=localhost;Database=ELearning_DB;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models --context-dir Data -c AppDbContext --force
