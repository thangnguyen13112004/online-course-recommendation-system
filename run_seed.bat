@echo off
echo ====================================================
echo EDU LERNING DB SEEDER
echo ====================================================
echo Dang nap du lieu mau (55MB) vao database ELearning_DB...
echo Qua trinh nay co the mat tu 1-2 phut, vui long khong tat cua so nay.

sqlcmd -S localhost,1433 -U sa -P "StrongPass@123" -d ELearning_DB -f 65001 -C -i Data_Seed.sql

echo.
echo HOAN TAT! Du lieu da duoc import vao Database thanh cong.
pause
