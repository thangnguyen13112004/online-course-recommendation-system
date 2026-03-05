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
			```

		#### With file mock_user_interactio.csv, command of cypher:
			```cypher
				// 1. Đọc file CSV tương tác người dùng
					LOAD CSV WITH HEADERS FROM 'file:///mock_user_interactio.csv' AS row
					// --- THÊM DÒNG NÀY ĐỂ BỘ LỌC DỮ LIỆU RỖNG ---
					WITH row WHERE row.UserID IS NOT NULL AND row.CourseID IS NOT NULL AND row.InteractionType IS NOT NULL
					// 2. Tạo nút Người dùng (User)
					MERGE (u:User {id: toInteger(row.UserID)})
					// 3. Tạo mối quan hệ tương tác giữa Người dùng và Khóa học
					MATCH (c:Course {id: toInteger(row.CourseID)})
					MERGE (u)-[r:INTERACTS_WITH {type: row.InteractionType}]->(c)
			```

### 3. Access in Controller/RecommendationController.cs, change password or set up password on your NeO4J.