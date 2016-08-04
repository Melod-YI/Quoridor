package model.impl;

import model.po.BlockPO;
import model.po.PlayerPO;
import model.po.WallPO;
import abstracter.Direction;
import abstracter.WallDirection;
import model.impl.BaseModel;
import model.service.ChessBoardModelService;
import model.service.GameModelService;
import model.state.BlockState;
import model.state.WallState;

public class ChessBoardModelImpl extends BaseModel implements ChessBoardModelService{
	private BlockPO[][] blockMatrix;
	private PlayerPO[] playerMatrix;
	private WallPO[][] wallMatrixX;//����Ϊ���������ǽ
	private WallPO[][] wallMatrixY;//����Ϊ���������ǽ
	private GameModelService gameModel;
	private int playerNum;
	public static boolean Server = false;
	public static boolean Client = false;
	private int width;
	private int height;
	private int wallNum;
	@Override
	public boolean initialize(int height,int width,int wallNum,int playerNum) {
		// TODO Auto-generated method stub
		this.height=height;
		this.width=width;
		this.wallNum=wallNum;
		this.playerNum=playerNum;
		blockMatrix = new BlockPO[width][height];//�����̸�����
		this.playerNum=playerNum;//���������
		playerMatrix = new PlayerPO[this.playerNum];//���������
		wallMatrixX = new WallPO[width][height+1];
		wallMatrixY = new WallPO[width+1][height];//����������
		for(int i=0;i<width;i++){
			wallMatrixX[i][height].setState(WallState.red);
			wallMatrixX[i][0].setState(WallState.red);
		}
		for(int i=0;i<height;i++){
			wallMatrixY[0][i].setState(WallState.red);
			wallMatrixY[width][i].setState(WallState.red);
		}//��ʼ��������߽�
		for(int i=0;i<playerNum;i++){
			playerMatrix[i].setPlayNo(i);
			playerMatrix[i].setWallLeft(this.wallNum);
		}//��ʼ�����
		for(int i=0;i<width;i++){
			for(int j=0;j<height;j++){
				blockMatrix[i][j].setX(i);
				blockMatrix[i][j].setY(j);
				blockMatrix[i][j].setState(BlockState.empty);
			}
		}//��ʼ�����̸�
		detailInit(height,width,playerNum);//��������������ѡȡ��
		return false;
	}

	private void detailInit(int height,int width,int playerNum){
		if(playerNum==2){
			blockMatrix[width/2][0].setState(BlockState.red);
			blockMatrix[0][height/2].setState(BlockState.blue);
		}
		if(playerNum>=3){
			blockMatrix[width/2][0].setState(BlockState.red);
			blockMatrix[0][height/2].setState(BlockState.yellow);
			blockMatrix[width/2][height-1].setState(BlockState.blue);
			if(playerNum==4){
				blockMatrix[width-1][height/2].setState(BlockState.green);
			}
		}
		//��ҵ���ɫ˳��̶�Ϊred,blue,yellow,green
	}
	
	
	@Override
	public void setGameModel(GameModelService gameModel) {
		this.gameModel = gameModel;
		// TODO Auto-generated method stub
	}

	@Override
	public boolean move(int playerNo,Direction direction) {
		// TODO Auto-generated method stub
		int x=0;
		int y=0;
		BlockState bs=getPlayerColor(playerNo);//��ȡ������ɫ
		for(int i=0;i<width;i++){
			for(int j=0;j<height;j++){
				if(blockMatrix[i][j].getState()==bs){
					x=i;
					y=j;
				}
			}
		}//��ȡ��������
		switch(direction){//�����ƶ������ж��Ƿ�����ƶ��������������1.�ƶ��������Ƿ���ǽ���߽�����ǽ����2.�ƶ��������м�������
		case up:
			if(wallMatrixX[x][y+1].getState()==WallState.black){//�ж��ƶ��������Ƿ���ǽ
				if(blockMatrix[x][y+1].getState()==BlockState.empty){//�ƶ�������ûǽ���ƶ�������û��
					blockMatrix[x][y].setState(BlockState.empty);
					blockMatrix[x][y+1].setState(bs);
					return true;
				}
				else if(wallMatrixX[x][y+2].getState()==WallState.black
						&&blockMatrix[x][y+2].getState()==BlockState.empty){//�������һ���Ҹ����Ϸ�Ҳû��ǽ
					blockMatrix[x][y].setState(BlockState.empty);
					blockMatrix[x][y+2].setState(bs);
					return true;
				}
			}
			break;
		case down:
			if(wallMatrixX[x][y].getState()==WallState.black){//�ж��ƶ��������Ƿ���ǽ
				if(blockMatrix[x][y-1].getState()==BlockState.empty){//�ƶ�������ûǽ���ƶ�������û��
					blockMatrix[x][y].setState(BlockState.empty);
					blockMatrix[x][y-1].setState(bs);
					return true;
				}
				else if(wallMatrixX[x][y-1].getState()==WallState.black
						&&blockMatrix[x][y-2].getState()==BlockState.empty){//�������һ���Ҹ����Ϸ�Ҳû��ǽ
					blockMatrix[x][y].setState(BlockState.empty);
					blockMatrix[x][y-2].setState(bs);
					return true;
				}
			}
			break;
		case left:
			if(wallMatrixY[x][y].getState()==WallState.black){//�ж��ƶ��������Ƿ���ǽ
				if(blockMatrix[x-1][y].getState()==BlockState.empty){//�ƶ�������ûǽ���ƶ�������û��
					blockMatrix[x][y].setState(BlockState.empty);
					blockMatrix[x-1][y].setState(bs);
					return true;
				}
				else if(wallMatrixY[x-1][y].getState()==WallState.black
						&&blockMatrix[x-2][y].getState()==BlockState.empty){//�������һ���Ҹ����Ϸ�Ҳû��ǽ
					blockMatrix[x][y].setState(BlockState.empty);
					blockMatrix[x-2][y].setState(bs);
					return true;
				}
			}
			break;
		case right:
			if(wallMatrixY[x+1][y].getState()==WallState.black){//�ж��ƶ��������Ƿ���ǽ
				if(blockMatrix[x+1][y].getState()==BlockState.empty){//�ƶ�������ûǽ���ƶ�������û��
					blockMatrix[x][y].setState(BlockState.empty);
					blockMatrix[x+1][y].setState(bs);
					return true;
				}
				else if(wallMatrixY[x+2][y].getState()==WallState.black
						&&blockMatrix[x+2][y].getState()==BlockState.empty){//�������һ���Ҹ����Ϸ�Ҳû��ǽ
					blockMatrix[x][y].setState(BlockState.empty);
					blockMatrix[x+2][y].setState(bs);
					return true;
				}
			}
			break;
		default:
			break; 
		}
		return false;
	}
	
	
	private BlockState getPlayerColor(int playerNo){
		BlockState bs=null;
		if(playerNo==1){
			bs=BlockState.red;
		}
		else if(playerNo==2){
			bs=BlockState.blue;
		}
		else if(playerNo==3){
			bs=BlockState.yellow;
		}
		else if(playerNo==4){
			bs=BlockState.green;
		}
		return bs;
	}

	@Override
	public boolean set(int playerNo,int x, int y, WallDirection wallDirection) {//��ǽ�жϷ��������Ƿ�������ǽ�ص�(�����Ƿ�������ص�)���Ƿ�Ȧ������
		if(playerMatrix[playerNo-1].getWallLeft()<=0){
			return false;
		}
		if(x==0||y==0||x==width||y==height){
			return false;
		}
		if(wallDirection==WallDirection.horizontal){
			if(wallMatrixX[x][y].getState()==WallState.black||wallMatrixX[x][y-1].getState()==WallState.black){
				if(wallMatrixY[x][y].getState()==WallState.black&&wallMatrixY[x-1][y].getState()==WallState.black){
					wallMatrixY[x][y].setState(WallState.red);
					wallMatrixY[x-1][y].setState(WallState.red);
					return true;
				}
			}
		}
		else{
			if(wallMatrixY[x][y].getState()==WallState.black||wallMatrixY[x-1][y].getState()==WallState.black){
				if(wallMatrixX[x][y].getState()==WallState.black&&wallMatrixX[x][y-1].getState()==WallState.black){
					wallMatrixX[x][y].setState(WallState.red);
					wallMatrixX[x][y-1].setState(WallState.red);
					return true;
				}
			}
		}
		return false;
	}//��ʱû���ǳɻ�����
}
