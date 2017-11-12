<template>
  <v-card class='account-content'>
    <v-card-text class="grey darken-4">
      API Token: <span class='caption'>{{ account.apiToken }}</span>
      <br>
      URL: <span class='caption'>{{ account.restApi }}</span>
      <br>      
      <v-dialog v-model="deleteDialog" persistent style='width: 100%;'>
        <v-btn block flat small color='red' dark slot="activator">delete</v-btn>
        <v-card>
          <v-card-title class="headline">Are you sure?</v-card-title>
          <v-card-text>This will permanently delete this account, and there's no undo button.</v-card-text>
          <v-card-actions>
            <v-spacer></v-spacer>
            <v-btn flat @click.native="deleteDialog=false">Cancel</v-btn>
            <v-btn color="red" flat @click.native="deleteAccount">Delete</v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>
    </v-card-text>
  </v-card>
</template>

<script>
export default {
  name: 'Account',
  props: {
    account: Object
  },
  data() {
    return {
      deleteDialog: false,
    }
  },
  methods: {
    deleteAccount() {
      Interop.removeAccount( this.account.fileName )
      this.$store.commit( 'DELETE_ACCOUNT', this.account.fileName )
    }
  }
}
</script>

<style lang="scss">
</style>