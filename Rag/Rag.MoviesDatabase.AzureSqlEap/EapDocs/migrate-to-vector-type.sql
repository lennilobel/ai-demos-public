/*
    -------------------------------------------------------------
    How to move from varbinary(8000) to vector type in SQL Server
    A step-by-step guide
    -------------------------------------------------------------
*/

/* 
    Assume that you have the following table in your database
    where the columns title_vector and content_vector are of type varbinary(8000)
    and used to store the embeddings of the title and content of the articles.
*/
create table [dbo].[wikipedia_articles_embeddings]
(
	[id] int not null,
	[url] varchar(1000) not null,
	[title] nvarchar(1000) not null,
	[text] nvarchar(max) not null,
	[title_vector] varbinary(8000) not null,
	[content_vector] varbinary(8000) not null
)
go

/*
    Step 1: Add new columns of type vector to store the embeddings
    using the new vector type. The vector type must be declared with
    the exact number of dimensions of the vectors that will be stored in it.
    Let's assume that vectors have 1536 dimensions.
*/
alter table wikipedia_articles_embeddings
add title_vector_2 vector(1536);
go
alter table wikipedia_articles_embeddings
add content_vector_2 vector(1536);
go

/*
    Step 2: Update the new columns with the values from the old columns.
    The values must be converted from varbinary to vector using a 2-phase process:
        1: convert varbinary to json array
        2: convert json array to vector

    If table is big, it is recommened to use a loop to update the values in batches
    to avoid transaction log growth and lock escalation. 
*/
while (1=1)	begin
    update top(1000)
        wikipedia_articles_embeddings
    set 
        title_vector_2 = cast(vector_to_json_array([title_vector]) as vector(1536)),
        content_vector_2 = cast(vector_to_json_array([content_vector]) as vector(1536))
    where
        title_vector_2 is null or content_vector_2 is null;
    
    if @@rowcount = 0
        break;
end
go

/*
    Step 3: Drop the old columns
*/
alter table wikipedia_articles_embeddings
drop column title_vector;
go
alter table wikipedia_articles_embeddings
drop column content_vector;
go

/*
    Step 4: Rename the new columns to the original names
*/
exec sp_rename 'dbo.wikipedia_articles_embeddings.title_vector_2', 'title_vector', 'COLUMN'
exec sp_rename 'dbo.wikipedia_articles_embeddings.content_vector_2', 'content_vector', 'COLUMN'
go

/*
    Done! Run some test query to verify that everything is working as expected
*/
select * from [dbo].[wikipedia_articles_embeddings]
where title = 'Alan Turing'
go

declare @v vector(1536)
select @v = title_vector from [dbo].[wikipedia_articles_embeddings]
where title = 'Isaac Asimov';

select top(10)
    id, 
    title,
    distance = vector_distance('cosine', @v, title_vector)  
from 
    dbo.wikipedia_articles_embeddings 
order by 
    distance
