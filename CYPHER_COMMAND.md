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

#   4. Content-based
        ### Bước 1: Xóa dữ liệu cũ (Cleanup)
			```cypher
				// 1. Tương ứng với hàm deleteContentSimilarRelationships
				MATCH ()-[r:CONTENT_SIMILAR]-()
				DELETE r;

				// 2. Tương ứng với hàm deleteProjection
				// Xóa projection có tên 'contentGraph' nếu nó đang tồn tại
				CALL gds.graph.drop('contentGraph', false);
			```
		### Bước 2: Tạo đồ thị ảo (Graph Projection)
			```cypher
				CALL gds.graph.project(
					'contentGraph',
					['Course','Topic'],
					['HAS_TOPIC']
				);
			```
		### Bước 3: Chạy thuật toán Node Similarity
			```cypher
				CALL gds.nodeSimilarity.write('contentGraph', {
					nodeLabels:['Course','Topic'],
					writeRelationshipType: 'CONTENT_SIMILAR',
					writeProperty: 'score',
					similarityCutoff: 0.5
				});
			```
		### A. Tìm các Course tương tự một Course cụ thể (Tương ứng getSimilarItems)
			```cypher
				MATCH (i:Course {id: 23})-[r:CONTENT_SIMILAR]-(q:Course)
				WHERE r.score >= 0.5
				RETURN DISTINCT q
				LIMIT 10;
			```
		### B. Gợi ý theo hồ sơ User (Tương ứng getProfilePageItems)
			```cypher
				MATCH (u:User {id: 1})-[r:RATED]->(i:Course)
				WITH i, COLLECT(i) as ratedItems, (r.rating - 1) / 4.0 as normalizedRating
				ORDER BY normalizedRating DESC
				LIMIT 5

				MATCH (i)-[rel:CONTENT_SIMILAR]-(q:Course)
				WHERE NOT q IN ratedItems
				WITH q, rel.score + 1.5 * normalizedRating as simScore
				ORDER BY simScore DESC
				WITH DISTINCT q, simScore
				LIMIT 10
				RETURN q.id as itemId, q.title as title, simScore as score;
			```
		
		