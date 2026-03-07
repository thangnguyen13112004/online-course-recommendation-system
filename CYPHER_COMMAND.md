### 1. create database with name is course

		map data from 2 file csv, which include Online_Courses_Full.csv and mock_user_interactio.csv

### 2. command of cypher to create ralationship, node,...

		#### With file Online_Courses_Full.csv, command of cypher:

			```cypher
				// 1. Đọc file CSV khóa học
					LOAD CSV WITH HEADERS FROM 'file:///Online_Courses_Full.csv' AS row

				// --- THÊM DÒNG NÀY ĐỂ BỘ LỌC DỮ LIỆU RỖNG ---
				WITH row WHERE row.CourseID IS NOT NULL AND row.Category IS NOT NULL AND row.Instructors IS NOT NULL

				// 2. Tạo nút Khóa học (Course)
				MERGE (c:Course {id: toInteger(row.CourseID)})
				SET c.title = row.Title, 
					c.url = row.URL, 
					c.rating = toFloat(row.Rating)

				// 3. Tạo nút Danh mục (Category) và vẽ mũi tên [BELONGS_TO]
				MERGE (cat:Category {name: row.Category})
				MERGE (c)-[:BELONGS_TO]->(cat)

				// 4. Tạo nút Giảng viên (Instructor) và vẽ mũi tên [CONDUCTS]
				MERGE (i:Instructor {name: row.Instructors})
				MERGE (i)-[:CONDUCTS]->(c)

				// 5. TẠO NÚT TOPIC (TỪ CỘT SKILLS) VÀ VẼ MŨI TÊN [HAS_TOPIC]
				// Dùng WITH để mang biến c (Course) và row đi tiếp
				WITH c, row
				WHERE row.Skills IS NOT NULL

				// Tách chuỗi Skills bằng dấu phẩy và biến mỗi kỹ năng thành một dòng xử lý riêng
				UNWIND split(row.Skills, ',') AS skill

				// Loại bỏ khoảng trắng ở 2 đầu của chuỗi (trim) và bỏ qua các giá trị rỗng (do dấu phẩy dư)
				WITH c, trim(skill) AS topicName
				WHERE topicName <> ''

				// Tạo nút Topic và nối khóa học với Topic đó
				MERGE (t:Topic {name: topicName})
				MERGE (c)-[:HAS_TOPIC]->(t)
			```

		#### With file mock_user_interactio.csv, command of cypher:
			```cypher
				// 1. Đọc file CSV tương tác người dùng
				LOAD CSV WITH HEADERS FROM 'file:///mock_user_interactions.csv' AS row

				// 1. Lọc dữ liệu rỗng
				WITH row WHERE row.UserID IS NOT NULL AND row.CourseID IS NOT NULL AND row.Rating IS NOT NULL

				// 2. Tạo hoặc khớp User
				MERGE (u:User {id: toInteger(row.UserID)})

				WITH u, row

				// 3. Khớp Course
				MATCH (c:Course {id: toInteger(row.CourseID)})

				// 4. CHỈ MERGE MŨI TÊN (KHÔNG KÈM THUỘC TÍNH RATING VÀO ĐÂY)
				MERGE (u)-[r:RATED]->(c)

				// 5. CẬP NHẬT RATING
				SET r.rating = toInteger(row.Rating)
			```

### 3. Access in Controller/RecommendationController.cs, change password or set up password on your NeO4J.