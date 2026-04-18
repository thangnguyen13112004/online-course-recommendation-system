### 1. Create database with name is course

Map data from 2 file csv, which include Online_Courses_Full.csv and mock_user_interactio.csv

### 2. Command of cypher to create ralationship, node,...

#### Tạo Constraint (Chạy 1 lần trước khi Import):
```cypher
	// Việc này sẽ khóa id của Course và User lại, đảm bảo Neo4j tuyệt đối không bao giờ tạo ra 2 node có cùng ID, 
	//đồng thời tăng tốc độ import lên gấp nhiều lần.
	CREATE CONSTRAINT course_id IF NOT EXISTS FOR (c:Course) REQUIRE c.id IS UNIQUE;
	CREATE CONSTRAINT user_id IF NOT EXISTS FOR (u:User) REQUIRE u.id IS UNIQUE;				
```
#### Import Online_Courses_Full.csv:
```cypher
	// 1. Đọc file CSV khóa học
	LOAD CSV WITH HEADERS FROM 'file:///Online_Courses_Full.csv' AS row

	// Bộ lọc rỗng
	WITH row WHERE row.CourseID IS NOT NULL AND row.Category IS NOT NULL AND row.Instructors IS NOT NULL

	// --- BỘ LỌC LOẠI BỎ TRÙNG LẶP ---
	// Gom nhóm theo Title. Nếu file CSV có 5 dòng trùng Title, nó chỉ lấy cấu trúc của dòng đầu tiên để import
	WITH row.Title AS courseTitle, collect(row)[0] AS row

	// 2. Tạo nút Khóa học (Course)
	MERGE (c:Course {id: toInteger(row.CourseID)})
	SET c.title = courseTitle, 
		c.url = row.URL, 
		c.rating = toFloat(row.Rating)

	// 3. Tạo nút Danh mục (Category)
	MERGE (cat:Category {name: row.Category})
	MERGE (c)-[:BELONGS_TO]->(cat)

	// 4. Tạo nút Giảng viên (Instructor) - Đã sửa lỗi nhiều giảng viên chung 1 node
	WITH c, row 
	UNWIND split(row.Instructors, ',') AS inst
	WITH c, row, trim(inst) AS instructorName
	WHERE instructorName <> ''

	MERGE (i:Instructor {name: instructorName})
	MERGE (i)-[:CONDUCTS]->(c)

	// 5. Tạo nút Topic (Từ cột Skills)
	WITH c, row
	WHERE row.Skills IS NOT NULL
	UNWIND split(row.Skills, ',') AS skill
	WITH c, trim(skill) AS topicName
	WHERE topicName <> ''

	MERGE (t:Topic {name: topicName})
	MERGE (c)-[:HAS_TOPIC]->(t)
```

#### Import mock_user_interactio.csv:
```cypher
	// 1. Đọc file CSV tương tác người dùng
	LOAD CSV WITH HEADERS FROM 'file:///mock_user_interactions.csv' AS row

	// 2. Lọc dữ liệu rỗng
	WITH row WHERE row.UserID IS NOT NULL AND row.CourseID IS NOT NULL AND row.Rating IS NOT NULL

	// --- LỌC TRÙNG LẶP TƯƠNG TÁC ---
	// Đảm bảo 1 UserID và 1 CourseID chỉ lấy 1 dòng dữ liệu đánh giá CUỐI CÙNG
	WITH row.UserID AS uID, row.CourseID AS cID, collect(row)[-1] AS row

	// 3. Tạo hoặc khớp User
	MERGE (u:User {id: toInteger(uID)})

	WITH u, row, cID

	// 4. Khớp Course
	// Những khóa học bị loại bỏ do trùng lặp (ở bước tạo Course) sẽ không được MATCH và lệnh sẽ an toàn bỏ qua
	MATCH (c:Course {id: toInteger(cID)})

	// 5. Tạo mũi tên (Chỉ MERGE mũi tên)
	MERGE (u)-[r:RATED]->(c)

	// 6. CẬP NHẬT RATING lấy từ dòng dữ liệu cuối cùng của user đó
	SET r.rating = toInteger(row.Rating)
```
		
### 3. Access in Controller/RecommendationController.cs, change password or set up password on your NeO4J.

### 4. Content-based
#### Bước 1: Xóa dữ liệu cũ (Cleanup)
```cypher
// 1. Tương ứng với hàm deleteContentSimilarRelationships
MATCH ()-[r:CONTENT_SIMILAR]-()
DELETE r;

// 2. Tương ứng với hàm deleteProjection
// Xóa projection có tên 'contentGraph' nếu nó đang tồn tại
CALL gds.graph.drop('contentGraph', false);
```
#### Bước 2: Tạo đồ thị ảo (Graph Projection)
```cypher
CALL gds.graph.project(
	'contentGraph',
	['Course','Topic'],
	['HAS_TOPIC']
);
```
#### Bước 3: Chạy thuật toán Node Similarity
```cypher
CALL gds.nodeSimilarity.write('contentGraph', {
	nodeLabels:['Course','Topic'],
	writeRelationshipType: 'CONTENT_SIMILAR',
	writeProperty: 'score',
	similarityCutoff: 0.5
});
```
#### A. Tìm các Course tương tự một Course cụ thể (Tương ứng getSimilarItems)
```cypher
MATCH (i:Course {id: 23})-[r:CONTENT_SIMILAR]-(q:Course)
WHERE r.score >= 0.5
RETURN DISTINCT q
LIMIT 10;
```
#### B. Gợi ý theo hồ sơ User (Tương ứng getProfilePageItems)
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

		
